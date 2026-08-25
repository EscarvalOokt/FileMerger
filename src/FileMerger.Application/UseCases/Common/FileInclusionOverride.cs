namespace FileMerger.Application.UseCases.Common;

public sealed record FileInclusionOverride
{
    public FileInclusionOverride(string fullPath, bool isIncluded)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Full path cannot be empty.", nameof(fullPath));

        FullPath = fullPath;
        IsIncluded = isIncluded;
    }

    public string FullPath { get; }
    public bool IsIncluded { get; }
}