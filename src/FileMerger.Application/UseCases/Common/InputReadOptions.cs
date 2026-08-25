using FileMerger.Domain.Enums;

namespace FileMerger.Application.UseCases.Common;

public sealed record InputReadOptions(
    InputEncodingMode EncodingMode,
    string? PreferredEncodingName,
    string? FallbackEncodingName);