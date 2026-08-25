namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogFileListResult(
    bool IsSuccessful,
    IReadOnlyCollection<CrashLogFileInfo> Files,
    Exception? Error)
{
    public static CrashLogFileListResult Success(
        IReadOnlyCollection<CrashLogFileInfo> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return new CrashLogFileListResult(true, files, null);
    }

    public static CrashLogFileListResult Failure(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new CrashLogFileListResult(false, [], error);
    }
}