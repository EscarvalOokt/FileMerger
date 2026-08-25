namespace FileMerger.Wpf.Features.Preview;

[Flags]
public enum PreviewDirtyReason
{
    None = 0,
    NeverBuilt = 1,
    SourcesChanged = 2,
    SessionSettingsChanged = 4,
    ProfileChanged = 8,
    FileOverridesChanged = 16
}