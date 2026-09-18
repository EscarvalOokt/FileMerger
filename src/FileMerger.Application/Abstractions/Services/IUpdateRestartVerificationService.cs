using FileMerger.Application.Updates;

namespace FileMerger.Application.Abstractions.Services;

public interface IUpdateRestartVerificationService
{
    Task<UpdateRestartVerificationOutcome> PrepareStartupVerificationAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);

    Task<UpdateRestartVerificationOutcome> CompleteStartupVerificationAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}