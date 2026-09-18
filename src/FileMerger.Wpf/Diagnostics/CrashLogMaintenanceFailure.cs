namespace FileMerger.Wpf.Diagnostics;

// ReSharper disable NotAccessedPositionalProperty.Global
public sealed record CrashLogMaintenanceFailure(string Path, Exception Error);