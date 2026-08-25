using FileMerger.Application.Abstractions.Services;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.SaveOutput;

public sealed class SaveMergeOutputUseCase
{
    private readonly IMergeWriter _mergeWriter;

    public SaveMergeOutputUseCase(IMergeWriter mergeWriter)
    {
        ArgumentNullException.ThrowIfNull(mergeWriter);
        _mergeWriter = mergeWriter;
    }

    public SaveMergeOutputResult Execute(SaveMergeOutputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ValidationIssue> issues = [];

        if (string.IsNullOrWhiteSpace(request.Target.Path))
        {
            issues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "output.path.empty",
                "Output path cannot be empty."));
        }

        if (issues.Count > 0)
        {
            return new SaveMergeOutputResult(
                isSuccessful: false,
                target: request.Target,
                validationIssues: issues);
        }

        _mergeWriter.Write(request.Output, request.Target);

        return new SaveMergeOutputResult(
            isSuccessful: true,
            target: request.Target);
    }
}