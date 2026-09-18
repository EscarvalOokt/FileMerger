namespace FileMerger.Wpf.Features.Settings;

public sealed record ApplicationPreferencesDocumentDto(int SchemaVersion, ApplicationPreferencesDto? Preferences);