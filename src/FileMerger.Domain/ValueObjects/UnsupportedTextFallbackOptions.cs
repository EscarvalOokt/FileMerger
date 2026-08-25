namespace FileMerger.Domain.ValueObjects;

public sealed record UnsupportedTextFallbackOptions
{
    public const long DefaultMaxFileSizeBytes = 1_048_576;
    public const int DefaultProbeSizeBytes = 16_384;
    public const double DefaultMaxControlCharacterRatio = 0.10d;

    public static UnsupportedTextFallbackOptions Disabled { get; } = new(
        isEnabled: false);

    public static UnsupportedTextFallbackOptions Enabled { get; } = new(
        isEnabled: true);

    public UnsupportedTextFallbackOptions(
        bool isEnabled = false,
        long maxFileSizeBytes = DefaultMaxFileSizeBytes,
        int probeSizeBytes = DefaultProbeSizeBytes,
        double maxControlCharacterRatio = DefaultMaxControlCharacterRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxFileSizeBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(probeSizeBytes);

        if (maxControlCharacterRatio < 0 || maxControlCharacterRatio > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxControlCharacterRatio),
                "Max control character ratio must be between 0 and 1.");
        }

        IsEnabled = isEnabled;
        MaxFileSizeBytes = maxFileSizeBytes;
        ProbeSizeBytes = probeSizeBytes;
        MaxControlCharacterRatio = maxControlCharacterRatio;
    }

    public bool IsEnabled { get; }
    public long MaxFileSizeBytes { get; }
    public int ProbeSizeBytes { get; }
    public double MaxControlCharacterRatio { get; }
}