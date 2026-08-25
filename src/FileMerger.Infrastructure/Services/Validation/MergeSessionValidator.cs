using System.Text;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Infrastructure.Common;

namespace FileMerger.Infrastructure.Services.Validation;

public sealed class MergeSessionValidator : IMergeSessionValidator
{
    public IReadOnlyCollection<ValidationIssue> Validate(MergeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        List<ValidationIssue> issues = [];

        ValidateSession(session, issues);
        ValidateSources(session, issues);
        ValidateProfile(session, issues);
        ValidateOutput(session, issues);

        return issues;
    }

    private static void ValidateSession(
        MergeSession session,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(session.Name))
        {
            issues.Add(Error(
                "session.name.empty",
                "Session name cannot be empty."));
        }
    }

    private static void ValidateSources(
        MergeSession session,
        List<ValidationIssue> issues)
    {
        if (session.Sources.Count == 0)
        {
            issues.Add(Error(
                "session.sources.empty",
                "At least one source must be specified."));
            return;
        }

        MergeSource[] enabledSources = [.. session.Sources.Where(x => x.IsEnabled)];

        if (enabledSources.Length == 0)
        {
            issues.Add(Error(
                "session.sources.noneEnabled",
                "At least one enabled source must be specified."));
            return;
        }

        Dictionary<string, MergeSource> normalizedMap = new(StringComparer.OrdinalIgnoreCase);

        foreach (MergeSource source in enabledSources)
        {
            if (string.IsNullOrWhiteSpace(source.Path))
            {
                issues.Add(Error(
                    "source.path.empty",
                    "Source path cannot be empty."));
                continue;
            }

            string? normalizedPath = PathUtility.TryNormalize(source.Path);
            if (normalizedPath is null)
            {
                issues.Add(Error(
                    "source.path.invalid",
                    $"Source path is invalid: '{source.Path}'."));
                continue;
            }

            if (!normalizedMap.TryAdd(normalizedPath, source))
            {
                issues.Add(Warning(
                    "source.path.duplicate",
                    $"Duplicate source path detected: '{normalizedPath}'."));
            }

            switch (source.Type)
            {
                case MergeSourceType.Directory:
                    if (!Directory.Exists(normalizedPath))
                    {
                        issues.Add(Error(
                            "source.directory.notFound",
                            $"Source directory was not found: '{normalizedPath}'."));
                    }

                    break;

                case MergeSourceType.File:
                    if (!File.Exists(normalizedPath))
                    {
                        issues.Add(Error(
                            "source.file.notFound",
                            $"Source file was not found: '{normalizedPath}'."));
                    }

                    break;
            }
        }

        KeyValuePair<string, MergeSource>[] normalizedSources = [.. normalizedMap];

        for (int i = 0; i < normalizedSources.Length; i++)
        {
            for (int j = i + 1; j < normalizedSources.Length; j++)
            {
                string leftPath = normalizedSources[i].Key;
                string rightPath = normalizedSources[j].Key;

                MergeSource left = normalizedSources[i].Value;
                MergeSource right = normalizedSources[j].Value;

                if (left.Type != MergeSourceType.Directory &&
                    right.Type != MergeSourceType.Directory)
                {
                    continue;
                }

                if (PathUtility.PathsOverlap(leftPath, rightPath))
                {
                    issues.Add(Warning(
                        "source.path.overlap",
                        $"Sources overlap and may lead to duplicate discovery: '{leftPath}' and '{rightPath}'."));
                }
            }
        }
    }

    private static void ValidateProfile(
        MergeSession session,
        List<ValidationIssue> issues)
    {
        if (session.Profile.FileTypes.Count == 0 ||
            !session.Profile.FileTypes.Any(x => x.IsEnabled))
        {
            issues.Add(Error(
                "profile.fileTypes.empty",
                "At least one enabled file type must be selected."));
        }

        GeneralMergeOptions options = session.Profile.GeneralOptions;

        if (options.InputEncodingMode == InputEncodingMode.Specific &&
            string.IsNullOrWhiteSpace(options.PreferredInputEncodingName))
        {
            issues.Add(Error(
                "profile.inputEncoding.preferred.empty",
                "Preferred input encoding must be specified when input encoding mode is Specific."));
        }

        if (!string.IsNullOrWhiteSpace(options.PreferredInputEncodingName) &&
            !IsEncodingValid(options.PreferredInputEncodingName))
        {
            issues.Add(Error(
                "profile.inputEncoding.preferred.invalid",
                $"Preferred input encoding is invalid: '{options.PreferredInputEncodingName}'."));
        }

        if (!string.IsNullOrWhiteSpace(options.FallbackInputEncodingName) &&
            !IsEncodingValid(options.FallbackInputEncodingName))
        {
            issues.Add(Error(
                "profile.inputEncoding.fallback.invalid",
                $"Fallback input encoding is invalid: '{options.FallbackInputEncodingName}'."));
        }

        if (!string.IsNullOrWhiteSpace(options.PreferredInputEncodingName) &&
            !string.IsNullOrWhiteSpace(options.FallbackInputEncodingName) &&
            string.Equals(
                options.PreferredInputEncodingName,
                options.FallbackInputEncodingName,
                StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Warning(
                "profile.inputEncoding.redundantFallback",
                "Preferred and fallback input encodings are identical."));
        }
    }

    private static void ValidateOutput(
        MergeSession session,
        List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(session.OutputTarget.Path))
        {
            issues.Add(Error(
                "output.path.empty",
                "Output path cannot be empty."));
            return;
        }

        string? normalizedOutputPath = PathUtility.TryNormalize(session.OutputTarget.Path);
        if (normalizedOutputPath is null)
        {
            issues.Add(Error(
                "output.path.invalid",
                $"Output path is invalid: '{session.OutputTarget.Path}'."));
            return;
        }

        if (string.IsNullOrWhiteSpace(session.OutputTarget.EncodingName))
        {
            issues.Add(Error(
                "output.encoding.empty",
                "Output encoding cannot be empty."));
        }
        else if (!IsEncodingValid(session.OutputTarget.EncodingName))
        {
            issues.Add(Error(
                "output.encoding.invalid",
                $"Output encoding is invalid: '{session.OutputTarget.EncodingName}'."));
        }

        foreach (MergeSource source in session.Sources.Where(x => x.IsEnabled))
        {
            string? normalizedSourcePath = PathUtility.TryNormalize(source.Path);
            if (normalizedSourcePath is null)
                continue;

            if (source.Type == MergeSourceType.File &&
                PathUtility.PathEquals(normalizedSourcePath, normalizedOutputPath))
            {
                issues.Add(Warning(
                    "output.conflicts.withSourceFile",
                    "Output path matches one of the enabled file sources."));
            }

            if (source.Type == MergeSourceType.Directory &&
                PathUtility.IsPathInsideDirectory(normalizedOutputPath, normalizedSourcePath))
            {
                issues.Add(Warning(
                    "output.insideSourceDirectory",
                    "Output path is inside an enabled source directory and may be rediscovered later."));
            }
        }
    }

    private static ValidationIssue Error(string code, string message)
        => new(ValidationSeverity.Error, code, message);

    private static ValidationIssue Warning(string code, string message)
        => new(ValidationSeverity.Warning, code, message);

    private static bool IsEncodingValid(string encodingName)
    {
        try
        {
            _ = Encoding.GetEncoding(encodingName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}