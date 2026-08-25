using FileMerger.Domain.Entities;

namespace FileMerger.Wpf.Features.Preview.State;

public interface IAppliedPreviewFileStateStore
{
    bool HasAppliedState { get; }

    IReadOnlyDictionary<string, bool> Current { get; }

    void Set(IReadOnlyCollection<InputFile> files);

    void Clear();
}