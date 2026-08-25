using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.SaveOutput;

public sealed record SaveMergeOutputRequest
{
    public SaveMergeOutputRequest(MergeOutput output, OutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(target);

        Output = output;
        Target = target;
    }

    public MergeOutput Output { get; }
    public OutputTarget Target { get; }
}