using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Preview.State;

public sealed class PreviewDirtyStateTracker : ViewModelBase
{
    private bool _isPreviewDirty = true;
    private PreviewStateSnapshot? _lastAppliedState;
    private PreviewDirtyReason _previewDirtyReason = PreviewDirtyReason.NeverBuilt;

    public bool IsPreviewDirty
    {
        get => _isPreviewDirty;
        private set
        {
            if (SetProperty(ref _isPreviewDirty, value))
            {
                OnPropertyChanged(nameof(PreviewDirtySummary));
                OnPropertyChanged(nameof(PreviewDirtyTooltip));
            }
        }
    }

    public PreviewDirtyReason PreviewDirtyReason
    {
        get => _previewDirtyReason;
        private set
        {
            if (SetProperty(ref _previewDirtyReason, value))
            {
                OnPropertyChanged(nameof(PreviewDirtySummary));
                OnPropertyChanged(nameof(PreviewDirtyTooltip));
            }
        }
    }

    public bool HasAppliedPreview => _lastAppliedState is not null;

    public string PreviewDirtySummary
    {
        get
        {
            if (!IsPreviewDirty)
                return string.Empty;

            if (PreviewDirtyReason == PreviewDirtyReason.NeverBuilt)
                return "Preview has not been built yet.";

            List<string> parts = BuildReasonParts();
            if (parts.Count == 0)
                return "Preview is outdated.";

            return $"Preview is outdated: {string.Join(", ", parts)}.";
        }
    }

    public string PreviewDirtyTooltip
    {
        get
        {
            if (!IsPreviewDirty)
                return "Preview is up to date.";

            if (PreviewDirtyReason == PreviewDirtyReason.NeverBuilt)
                return "Preview has not been built yet. Build it to review the current merged output.";

            List<string> parts = BuildReasonParts();
            if (parts.Count == 0)
                return "Preview is outdated.";

            return $"Preview needs rebuild because {string.Join(", ", parts)}.";
        }
    }

    public void Reset()
    {
        bool hadAppliedPreview = HasAppliedPreview;

        _lastAppliedState = null;

        if (hadAppliedPreview)
            OnPropertyChanged(nameof(HasAppliedPreview));

        UpdateDirtyState(isPreviewDirty: true, previewDirtyReason: PreviewDirtyReason.NeverBuilt);
    }

    public void MarkPreviewApplied(PreviewStateSnapshot appliedState)
    {
        ArgumentNullException.ThrowIfNull(appliedState);

        bool hadAppliedPreview = HasAppliedPreview;

        _lastAppliedState = appliedState;

        if (hadAppliedPreview != HasAppliedPreview)
            OnPropertyChanged(nameof(HasAppliedPreview));

        UpdateDirtyState(isPreviewDirty: false, previewDirtyReason: PreviewDirtyReason.None);
    }

    public void Refresh(PreviewStateSnapshot currentState)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        if (_lastAppliedState is null)
        {
            UpdateDirtyState(isPreviewDirty: true, previewDirtyReason: PreviewDirtyReason.NeverBuilt);
            return;
        }

        PreviewDirtyReason reasons = PreviewDirtyReason.None;

        if (!SourceSnapshotsEqual(currentState.Sources, _lastAppliedState.Sources))
            reasons |= PreviewDirtyReason.SourcesChanged;

        if (currentState.Session != _lastAppliedState.Session)
            reasons |= PreviewDirtyReason.SessionSettingsChanged;

        if (!Equals(currentState.Profile, _lastAppliedState.Profile))
            reasons |= PreviewDirtyReason.ProfileChanged;

        if (!DictionariesEqual(currentState.FileInclusionOverrides, _lastAppliedState.FileInclusionOverrides))
            reasons |= PreviewDirtyReason.FileOverridesChanged;

        UpdateDirtyState(isPreviewDirty: reasons != PreviewDirtyReason.None, previewDirtyReason: reasons);
    }

    private void UpdateDirtyState(bool isPreviewDirty, PreviewDirtyReason previewDirtyReason)
    {
        IsPreviewDirty = isPreviewDirty;
        PreviewDirtyReason = previewDirtyReason;
    }

    private List<string> BuildReasonParts()
    {
        List<string> parts = [];

        if (PreviewDirtyReason.HasFlag(PreviewDirtyReason.SourcesChanged))
            parts.Add("sources changed");

        if (PreviewDirtyReason.HasFlag(PreviewDirtyReason.SessionSettingsChanged))
            parts.Add("session settings changed");

        if (PreviewDirtyReason.HasFlag(PreviewDirtyReason.ProfileChanged))
            parts.Add("profile changed");

        if (PreviewDirtyReason.HasFlag(PreviewDirtyReason.FileOverridesChanged))
            parts.Add("file overrides changed");

        return parts;
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

            if (!string.Equals(
                    leftExclusion.RelativePath,
                    rightExclusion.RelativePath,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            if (leftExclusion.Type != rightExclusion.Type || leftExclusion.IsEnabled != rightExclusion.IsEnabled)
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