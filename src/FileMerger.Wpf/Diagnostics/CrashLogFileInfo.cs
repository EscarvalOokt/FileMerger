namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogFileInfo(
    string Path,
    string FileName,
    DateTime LastWriteTimeUtc,
    long SizeInBytes);