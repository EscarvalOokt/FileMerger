namespace FileMerger.Application.Abstractions.Services;

public sealed record FileDiscoveryProgress(
    int ProbedFiles,
    string RelativePath);