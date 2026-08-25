namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogWriteResult(
    bool IsSuccessful,
    string? Path,
    Exception? Error)
{
    public static CrashLogWriteResult Success(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty.", nameof(path));

        return new CrashLogWriteResult(true, path, null);
    }

    public static CrashLogWriteResult Failure(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CrashLogWriteResult(false, null, error);
    }
}