using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.SaveOutput;

public sealed record SaveMergeOutputResult
{
    public SaveMergeOutputResult(
        bool isSuccessful,
        OutputTarget target,
        IReadOnlyCollection<ValidationIssue>? validationIssues = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        IsSuccessful = isSuccessful;
        Target = target;
        ValidationIssues = validationIssues ?? [];
    }

    public bool IsSuccessful { get; }
    public OutputTarget Target { get; }
    public IReadOnlyCollection<ValidationIssue> ValidationIssues { get; }
}