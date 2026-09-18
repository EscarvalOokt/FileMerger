using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Domain.Entities;

public sealed record InputFile
{
    public InputFile(
        string fullPath,
        string relativePath,
        string extension,
        FileKind kind,
        bool isIncluded = true,
        long? sizeInBytes = null,
        SkipReason? skipReason = null,
        bool isFallbackText = false,
        bool isMergeCandidate = true)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Full path cannot be empty.", nameof(fullPath));

        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));

        if (extension is null || (extension.Length > 0 && string.IsNullOrWhiteSpace(extension)))
        {
            throw new ArgumentException("Extension cannot be whitespace.", nameof(extension));
        }

        FullPath = fullPath;
        RelativePath = relativePath;
        Extension = extension;
        Kind = kind;
        IsIncluded = isIncluded;
        SizeInBytes = sizeInBytes;
        SkipReason = skipReason;
        IsFallbackText = isFallbackText;
        IsMergeCandidate = isMergeCandidate;
    }

    public string FullPath { get; }
    public string RelativePath { get; }
    public string Extension { get; }
    public FileKind Kind { get; }
    public bool IsIncluded { get; init; }
    public long? SizeInBytes { get; }
    public SkipReason? SkipReason { get; init; }
    public bool IsFallbackText { get; }
    public bool IsMergeCandidate { get; }

    public InputFile Exclude(SkipReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return this with { IsIncluded = false, SkipReason = reason };
    }

    public InputFile Include()
    {
        return this with { IsIncluded = true, SkipReason = null };
    }
}