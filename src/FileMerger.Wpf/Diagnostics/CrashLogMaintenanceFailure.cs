namespace FileMerger.Wpf.Diagnostics;

public sealed record CrashLogMaintenanceFailure(
    string Path,
    Exception Error);