using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Discovery;

public sealed class FileDiscoveryService : IFileDiscoveryService
{
    private readonly IUnsupportedTextFileDetector _unsupportedTextFileDetector;

    public FileDiscoveryService()
        : this(new UnsupportedTextFileDetector())
    {
    }

    public FileDiscoveryService(IUnsupportedTextFileDetector unsupportedTextFileDetector)
    {
        ArgumentNullException.ThrowIfNull(unsupportedTextFileDetector);

        _unsupportedTextFileDetector = unsupportedTextFileDetector;
    }

    public FileDiscoveryResult DiscoverFiles(
        IReadOnlyCollection<MergeSource> sources,
        MergeProfile profile,
        IProgress<FileDiscoveryProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(profile);

        Dictionary<string, FileTypeDefinition> knownTypes =
            BuildFileTypeLookup(profile.FileTypes);

        Dictionary<string, FileTypeDefinition> enabledTypes =
            BuildFileTypeLookup(profile.FileTypes.Where(x => x.IsEnabled));

        List<InputFile> inventoryFiles = [];
        List<InputFile> sourceExcludedFiles = [];
        var progressReporter = new DiscoveryProgressReporter(progress);

        bool shouldCollectSourceExcludedFiles = ShouldCollectSourceExcludedFiles(profile);

        foreach (MergeSource source in sources.Where(x => x.IsEnabled))
        {
            if (source.Type == MergeSourceType.Directory)
            {
                DiscoverFromDirectory(
                    source,
                    profile,
                    knownTypes,
                    enabledTypes,
                    inventoryFiles,
                    sourceExcludedFiles,
                    shouldCollectSourceExcludedFiles,
                    progressReporter);
            }
            else if (source.Type == MergeSourceType.File)
            {
                DiscoverFromFile(
                    source,
                    profile,
                    knownTypes,
                    enabledTypes,
                    inventoryFiles,
                    progressReporter);
            }
        }

        InputFile[] distinctInventoryFiles =
        [
            .. inventoryFiles
                .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
        ];

        HashSet<string> inventoryPaths = new(
            distinctInventoryFiles.Select(x => x.FullPath),
            StringComparer.OrdinalIgnoreCase);

        InputFile[] distinctSourceExcludedFiles =
        [
            .. sourceExcludedFiles
                .Where(x => !inventoryPaths.Contains(x.FullPath))
                .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
        ];

        return new FileDiscoveryResult(
            distinctInventoryFiles,
            distinctSourceExcludedFiles);
    }

    private static bool ShouldCollectSourceExcludedFiles(MergeProfile profile)
    {
        OutputMetadataOptions options = profile.GeneralOptions.OutputMetadataOptions;

        return options.SkippedFilesMetadataMode != SkippedFilesMetadataMode.None &&
               options.EffectiveSkippedFileCategories.Includes(
                   SkippedFileCategory.SourceExclusion);
    }

    private static Dictionary<string, FileTypeDefinition> BuildFileTypeLookup(
        IEnumerable<FileTypeDefinition> fileTypes)
    {
        return fileTypes
            .Where(x => !string.IsNullOrWhiteSpace(x.Extension))
            .GroupBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    private void DiscoverFromDirectory(
        MergeSource source,
        MergeProfile profile,
        IReadOnlyDictionary<string, FileTypeDefinition> knownTypes,
        IReadOnlyDictionary<string, FileTypeDefinition> enabledTypes,
        List<InputFile> result,
        List<InputFile> sourceExcludedFiles,
        bool shouldCollectSourceExcludedFiles,
        DiscoveryProgressReporter progressReporter)
    {
        if (!Directory.Exists(source.Path))
            return;

        string rootPath = Path.GetFullPath(source.Path);
        var exclusions = SourceExclusionMatcher.From(source, rootPath);

        foreach (string filePath in EnumerateFiles(source, rootPath, exclusions))
        {
            if (exclusions.IsFileExcluded(filePath))
                continue;

            AddDiscoveredFile(
                rootPath,
                filePath,
                profile,
                knownTypes,
                enabledTypes,
                result,
                progressReporter);
        }

        if (shouldCollectSourceExcludedFiles)
        {
            CollectSourceExcludedFiles(
                source,
                rootPath,
                sourceExcludedFiles);
        }
    }

    private static IEnumerable<string> EnumerateFiles(
        MergeSource source,
        string rootPath,
        SourceExclusionMatcher exclusions)
    {
        Stack<string> pendingDirectories = [];
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            string currentDirectory = pendingDirectories.Pop();

            foreach (string filePath in Directory.EnumerateFiles(currentDirectory))
            {
                yield return filePath;
            }

            if (!source.IsRecursive)
                continue;

            foreach (string directoryPath in Directory.EnumerateDirectories(currentDirectory))
            {
                if (exclusions.IsDirectoryExcluded(directoryPath))
                    continue;

                pendingDirectories.Push(directoryPath);
            }
        }
    }

    private static void CollectSourceExcludedFiles(
        MergeSource source,
        string rootPath,
        List<InputFile> result)
    {
        foreach (MergeSourceExclusion exclusion in source.Exclusions.Where(x => x.IsEnabled))
        {
            string excludedPath = Path.GetFullPath(Path.Combine(rootPath, exclusion.RelativePath));

            if (exclusion.Type == MergeSourceExclusionType.File)
            {
                if (!IsReachableFileExclusion(source, exclusion) || !File.Exists(excludedPath))
                    continue;

                result.Add(CreateSourceExcludedInputFile(
                    rootPath,
                    excludedPath));

                continue;
            }

            if (!source.IsRecursive || !Directory.Exists(excludedPath))
                continue;

            foreach (string filePath in EnumerateDirectoryTreeFiles(excludedPath))
            {
                result.Add(CreateSourceExcludedInputFile(
                    rootPath,
                    filePath));
            }
        }
    }

    private static bool IsReachableFileExclusion(
        MergeSource source,
        MergeSourceExclusion exclusion)
    {
        if (source.IsRecursive)
            return true;

        string? directoryName = Path.GetDirectoryName(exclusion.RelativePath);
        return string.IsNullOrWhiteSpace(directoryName);
    }

    private static IEnumerable<string> EnumerateDirectoryTreeFiles(string rootPath)
    {
        Stack<string> pendingDirectories = [];
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            string currentDirectory = pendingDirectories.Pop();

            foreach (string filePath in Directory.EnumerateFiles(currentDirectory))
                yield return filePath;

            foreach (string directoryPath in Directory.EnumerateDirectories(currentDirectory))
                pendingDirectories.Push(directoryPath);
        }
    }

    private static InputFile CreateSourceExcludedInputFile(
        string rootPath,
        string filePath)
    {
        return new InputFile(
            fullPath: filePath,
            relativePath: Path.GetRelativePath(rootPath, filePath),
            extension: ResolveUnsupportedExtension(filePath),
            kind: FileKind.Unknown,
            isIncluded: false,
            sizeInBytes: TryGetFileSize(filePath),
            skipReason: CreateSourceExclusionSkipReason(),
            isMergeCandidate: false);
    }

    private void AddDiscoveredFile(
        string rootPath,
        string filePath,
        MergeProfile profile,
        IReadOnlyDictionary<string, FileTypeDefinition> knownTypes,
        IReadOnlyDictionary<string, FileTypeDefinition> enabledTypes,
        List<InputFile> result,
        DiscoveryProgressReporter progressReporter)
    {
        string relativePath = Path.GetRelativePath(rootPath, filePath);

        result.Add(CreateInputFile(
            filePath,
            relativePath,
            profile.GeneralOptions.UnsupportedTextFallbackOptions,
            knownTypes,
            enabledTypes,
            progressReporter));
    }

    private void DiscoverFromFile(
        MergeSource source,
        MergeProfile profile,
        IReadOnlyDictionary<string, FileTypeDefinition> knownTypes,
        IReadOnlyDictionary<string, FileTypeDefinition> enabledTypes,
        List<InputFile> result,
        DiscoveryProgressReporter progressReporter)
    {
        if (!File.Exists(source.Path))
            return;

        result.Add(CreateInputFile(
            source.Path,
            Path.GetFileName(source.Path),
            profile.GeneralOptions.UnsupportedTextFallbackOptions,
            knownTypes,
            enabledTypes,
            progressReporter));
    }

    private InputFile CreateInputFile(
        string filePath,
        string relativePath,
        UnsupportedTextFallbackOptions fallbackOptions,
        IReadOnlyDictionary<string, FileTypeDefinition> knownTypes,
        IReadOnlyDictionary<string, FileTypeDefinition> enabledTypes,
        DiscoveryProgressReporter progressReporter)
    {
        if (TryResolveKnownFileType(
                filePath,
                enabledTypes,
                out string enabledExtension,
                out FileTypeDefinition enabledFileType))
        {
            return new InputFile(
                fullPath: filePath,
                relativePath: relativePath,
                extension: enabledExtension,
                kind: enabledFileType.Kind,
                sizeInBytes: TryGetFileSize(filePath));
        }

        if (TryResolveKnownFileType(
                filePath,
                knownTypes,
                out string knownExtension,
                out FileTypeDefinition knownFileType))
        {
            return new InputFile(
                fullPath: filePath,
                relativePath: relativePath,
                extension: knownExtension,
                kind: knownFileType.Kind,
                isIncluded: false,
                sizeInBytes: TryGetFileSize(filePath),
                skipReason: CreateDisabledFileTypeSkipReason(knownFileType),
                isMergeCandidate: false);
        }

        string unsupportedExtension = ResolveUnsupportedExtension(filePath);

        if (!string.IsNullOrWhiteSpace(unsupportedExtension) &&
            fallbackOptions.IsEnabled)
        {
            progressReporter.ReportProbe(relativePath);

            if (_unsupportedTextFileDetector.IsTextCandidate(filePath, fallbackOptions))
            {
                return new InputFile(
                    fullPath: filePath,
                    relativePath: relativePath,
                    extension: unsupportedExtension,
                    kind: FileKind.Text,
                    sizeInBytes: TryGetFileSize(filePath),
                    isFallbackText: true);
            }
        }

        return new InputFile(
            fullPath: filePath,
            relativePath: relativePath,
            extension: unsupportedExtension,
            kind: FileKind.Unknown,
            isIncluded: false,
            sizeInBytes: TryGetFileSize(filePath),
            skipReason: CreateUnsupportedFileTypeSkipReason(),
            isMergeCandidate: false);
    }

    private static bool TryResolveKnownFileType(
        string filePath,
        IReadOnlyDictionary<string, FileTypeDefinition> fileTypes,
        out string extension,
        out FileTypeDefinition fileType)
    {
        extension = Path.GetExtension(filePath);

        if (!string.IsNullOrWhiteSpace(extension) &&
            fileTypes.TryGetValue(extension, out FileTypeDefinition? extensionFileType))
        {
            fileType = extensionFileType;
            return true;
        }

        string fileName = Path.GetFileName(filePath);

        if (fileName.StartsWith('.') &&
            fileTypes.TryGetValue(fileName, out FileTypeDefinition? dotFileType))
        {
            extension = dotFileType.Extension;
            fileType = dotFileType;
            return true;
        }

        extension = string.Empty;
        fileType = null!;
        return false;
    }

    private static string ResolveUnsupportedExtension(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (!string.IsNullOrWhiteSpace(extension))
            return extension;

        string fileName = Path.GetFileName(filePath);
        if (fileName.StartsWith('.') && fileName.Length > 1)
            return fileName;

        return string.Empty;
    }

    private static SkipReason CreateDisabledFileTypeSkipReason(FileTypeDefinition fileType)
    {
        return new SkipReason(
            code: "discovery.file-type-disabled",
            description: $"File type '{fileType.DisplayName}' is disabled in the current profile.");
    }

    private static SkipReason CreateUnsupportedFileTypeSkipReason()
    {
        return new SkipReason(
            code: "discovery.unsupported-file-type",
            description: "File type is not supported by the current profile.");
    }

    private static SkipReason CreateSourceExclusionSkipReason()
    {
        return new SkipReason(
            code: "source.exclude",
            description: "Excluded by a source-specific exclusion.");
    }

    private static long? TryGetFileSize(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch
        {
            return null;
        }
    }

    private sealed class DiscoveryProgressReporter(IProgress<FileDiscoveryProgress>? progress)
    {
        private readonly IProgress<FileDiscoveryProgress>? _progress = progress;
        private int _probedFiles;

        public void ReportProbe(string relativePath)
        {
            _probedFiles++;
            _progress?.Report(new FileDiscoveryProgress(
                ProbedFiles: _probedFiles,
                RelativePath: relativePath));
        }
    }

    private sealed class SourceExclusionMatcher
    {
        private readonly string _rootPath;
        private readonly HashSet<string> _excludedFiles;
        private readonly HashSet<string> _excludedDirectories;

        private SourceExclusionMatcher(
            string rootPath,
            HashSet<string> excludedFiles,
            HashSet<string> excludedDirectories)
        {
            _rootPath = rootPath;
            _excludedFiles = excludedFiles;
            _excludedDirectories = excludedDirectories;
        }

        public static SourceExclusionMatcher From(MergeSource source, string rootPath)
        {
            ArgumentNullException.ThrowIfNull(source);

            HashSet<string> excludedFiles = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> excludedDirectories = new(StringComparer.OrdinalIgnoreCase);

            foreach (MergeSourceExclusion exclusion in source.Exclusions.Where(x => x.IsEnabled))
            {
                string relativePath = NormalizeRelativePath(exclusion.RelativePath);

                if (exclusion.Type == MergeSourceExclusionType.File)
                {
                    excludedFiles.Add(relativePath);
                }
                else if (exclusion.Type == MergeSourceExclusionType.Directory)
                {
                    excludedDirectories.Add(relativePath);
                }
            }

            return new SourceExclusionMatcher(
                rootPath: rootPath,
                excludedFiles: excludedFiles,
                excludedDirectories: excludedDirectories);
        }

        public bool IsFileExcluded(string filePath)
        {
            string relativePath = GetRelativePath(filePath);

            return _excludedFiles.Contains(relativePath) ||
                   IsInsideExcludedDirectory(relativePath);
        }

        public bool IsDirectoryExcluded(string directoryPath)
        {
            string relativePath = GetRelativePath(directoryPath);

            return IsSameOrInsideExcludedDirectory(relativePath);
        }

        private string GetRelativePath(string path)
        {
            return NormalizeRelativePath(Path.GetRelativePath(_rootPath, path));
        }

        private bool IsInsideExcludedDirectory(string relativeFilePath)
        {
            return _excludedDirectories.Any(excludedDirectory =>
                IsSameOrChildPath(relativeFilePath, excludedDirectory));
        }

        private bool IsSameOrInsideExcludedDirectory(string relativeDirectoryPath)
        {
            return _excludedDirectories.Any(excludedDirectory =>
                IsSameOrChildPath(relativeDirectoryPath, excludedDirectory));
        }

        private static bool IsSameOrChildPath(string candidate, string parent)
        {
            return string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase) ||
                   candidate.StartsWith(
                       parent + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeRelativePath(string path)
        {
            return path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .Trim(Path.DirectorySeparatorChar);
        }
    }
}