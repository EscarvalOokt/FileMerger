using System.Text.Json.Serialization;

namespace FileMerger.Wpf.Features.Profile.Models;

public sealed record ProfileMetadataDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("createdAtUtc")]
    DateTime? CreatedAtUtc,
    [property: JsonPropertyName("updatedAtUtc")]
    DateTime? UpdatedAtUtc,
    [property: JsonPropertyName("isBuiltIn")]
    bool IsBuiltIn = false,
    [property: JsonPropertyName("isReadOnly")]
    bool IsReadOnly = false);