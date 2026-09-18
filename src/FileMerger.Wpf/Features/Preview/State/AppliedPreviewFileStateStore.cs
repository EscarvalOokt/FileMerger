using FileMerger.Domain.Entities;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed class AppliedPreviewFileStateStore : IAppliedPreviewFileStateStore
{
    private static readonly IReadOnlyDictionary<string, bool> Empty =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyDictionary<string, bool>? _current;

    public bool HasAppliedState => _current is not null;

    public IReadOnlyDictionary<string, bool> Current => _current ?? Empty;

    public void Set(IReadOnlyCollection<InputFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        _current = files.GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().IsIncluded, StringComparer.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        _current = null;
    }
}