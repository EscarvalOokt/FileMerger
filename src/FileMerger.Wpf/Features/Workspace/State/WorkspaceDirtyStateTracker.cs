using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.State;

public sealed class WorkspaceDirtyStateTracker : ViewModelBase
{
    private WorkspaceDocumentStateSnapshot? _lastSavedState;
    private bool _isWorkspaceDirty;

    public bool IsWorkspaceDirty
    {
        get => _isWorkspaceDirty;
        private set
        {
            if (SetProperty(ref _isWorkspaceDirty, value))
            {
                OnPropertyChanged(nameof(WorkspaceDirtySummary));
                OnPropertyChanged(nameof(WorkspaceDirtyTooltip));
            }
        }
    }

    public bool HasSavedState => _lastSavedState is not null;

    public string WorkspaceDirtySummary =>
        IsWorkspaceDirty
            ? "Workspace has unsaved changes."
            : string.Empty;

    public string WorkspaceDirtyTooltip =>
        IsWorkspaceDirty
            ? "Workspace settings were changed after the last save."
            : "Workspace is saved.";

    public void Reset()
    {
        bool hadSavedState = HasSavedState;

        _lastSavedState = null;
        IsWorkspaceDirty = false;

        if (hadSavedState)
            OnPropertyChanged(nameof(HasSavedState));
    }

    public void MarkWorkspaceSaved(WorkspaceDocumentStateSnapshot savedState)
    {
        ArgumentNullException.ThrowIfNull(savedState);

        bool hadSavedState = HasSavedState;

        _lastSavedState = savedState;
        IsWorkspaceDirty = false;

        if (hadSavedState != HasSavedState)
            OnPropertyChanged(nameof(HasSavedState));
    }

    public void Refresh(WorkspaceDocumentStateSnapshot currentState)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        if (_lastSavedState is null)
        {
            IsWorkspaceDirty = true;
            return;
        }

        IsWorkspaceDirty = !WorkspaceStatesEqual(currentState, _lastSavedState);
    }

    private static bool WorkspaceStatesEqual(
        WorkspaceDocumentStateSnapshot left,
        WorkspaceDocumentStateSnapshot right)
    {
        PreviewStateSnapshot leftPreview = left.PreviewState;
        PreviewStateSnapshot rightPreview = right.PreviewState;

        return string.Equals(left.ProfileDisplayName, right.ProfileDisplayName, StringComparison.Ordinal) &&
               string.Equals(left.ProfileEntryId, right.ProfileEntryId, StringComparison.Ordinal) &&
               string.Equals(left.ProfileOriginEntryId, right.ProfileOriginEntryId, StringComparison.Ordinal) &&
               string.Equals(left.ProfileOriginDisplayName, right.ProfileOriginDisplayName, StringComparison.Ordinal) &&
               SourceSnapshotsEqual(leftPreview.Sources, rightPreview.Sources) &&
               leftPreview.Session == rightPreview.Session &&
               Equals(leftPreview.Profile, rightPreview.Profile) &&
               DictionariesEqual(leftPreview.FileInclusionOverrides, rightPreview.FileInclusionOverrides);
    }

    private static bool SourceSnapshotsEqual(
        IReadOnlyCollection<MergeSourceStateSnapshot> left,
        IReadOnlyCollection<MergeSourceStateSnapshot> right)
    {
        if (left.Count != right.Count)
            return false;

        using IEnumerator<MergeSourceStateSnapshot> leftEnumerator = left.GetEnumerator();
        using IEnumerator<MergeSourceStateSnapshot> rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext())
                return false;

            MergeSourceStateSnapshot leftSource = leftEnumerator.Current;
            MergeSourceStateSnapshot rightSource = rightEnumerator.Current;

            if (!string.Equals(leftSource.Path, rightSource.Path, StringComparison.OrdinalIgnoreCase))
                return false;

            if (leftSource.Type != rightSource.Type ||
                leftSource.IsRecursive != rightSource.IsRecursive ||
                leftSource.IsEnabled != rightSource.IsEnabled)
            {
                return false;
            }

            if (!ExclusionSnapshotsEqual(leftSource.Exclusions, rightSource.Exclusions))
                return false;
        }

        return !rightEnumerator.MoveNext();
    }

    private static bool ExclusionSnapshotsEqual(
        IReadOnlyCollection<MergeSourceExclusionStateSnapshot> left,
        IReadOnlyCollection<MergeSourceExclusionStateSnapshot> right)
    {
        if (left.Count != right.Count)
            return false;

        using IEnumerator<MergeSourceExclusionStateSnapshot> leftEnumerator = left.GetEnumerator();
        using IEnumerator<MergeSourceExclusionStateSnapshot> rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext())
                return false;

            MergeSourceExclusionStateSnapshot leftExclusion = leftEnumerator.Current;
            MergeSourceExclusionStateSnapshot rightExclusion = rightEnumerator.Current;

            if (!string.Equals(leftExclusion.RelativePath, rightExclusion.RelativePath, StringComparison.OrdinalIgnoreCase))
                return false;

            if (leftExclusion.Type != rightExclusion.Type ||
                leftExclusion.IsEnabled != rightExclusion.IsEnabled)
            {
                return false;
            }
        }

        return !rightEnumerator.MoveNext();
    }

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, bool> left,
        IReadOnlyDictionary<string, bool> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (KeyValuePair<string, bool> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out bool rightValue))
                return false;

            if (pair.Value != rightValue)
                return false;
        }

        return true;
    }
}