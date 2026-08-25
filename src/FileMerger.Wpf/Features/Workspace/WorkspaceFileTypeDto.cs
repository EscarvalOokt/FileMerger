using FileMerger.Domain.Enums;

namespace FileMerger.Wpf.Features.Workspace;

public sealed record WorkspaceFileTypeDto(
    string Extension,
    string DisplayName,
    FileKind Kind,
    bool IsEnabled,
    bool SupportsLanguageSpecificProcessing);