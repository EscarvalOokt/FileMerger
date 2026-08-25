using System.Text.Json.Serialization;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed record ProfileLibraryDocumentDto(
    [property: JsonPropertyName("schemaVersion")]
    int SchemaVersion,
    [property: JsonPropertyName("metadata")]
    ProfileMetadataDto Metadata,
    [property: JsonPropertyName("profile")]
    WorkspaceProfileDto Profile);