namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogFolderOpenResult(bool IsSuccessful, string DirectoryPath, Exception? Error)
{
    public static CrashLogFolderOpenResult Success(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));
        }

        return new CrashLogFolderOpenResult(true, directoryPath, null);
    }

    public static CrashLogFolderOpenResult Failure(string directoryPath, Exception error)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));
        }

        ArgumentNullException.ThrowIfNull(error);

        return new CrashLogFolderOpenResult(false, directoryPath, error);
    }
}