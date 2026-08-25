using System.Text;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Files.ViewModels;

public sealed class InputFileItemViewModel : ViewModelBase
{
    private bool _isIncluded;
    private bool _hasManualOverride;
    private bool _isAppliedInPreview;

    public InputFileItemViewModel(
        InputFile model,
        bool automaticIncluded,
        bool currentIncluded,
        bool appliedIncluded)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        AutomaticIncluded = model.IsMergeCandidate && automaticIncluded;
        AppliedIncluded = appliedIncluded;

        _isIncluded = model.IsMergeCandidate
            ? currentIncluded
            : AutomaticIncluded;

        _hasManualOverride = model.IsMergeCandidate &&
                             _isIncluded != AutomaticIncluded;

        _isAppliedInPreview = _isIncluded == appliedIncluded;
    }

    public InputFile Model { get; }

    public string RelativePath => Model.RelativePath;
    public string FullPath => Model.FullPath;
    public string Extension => Model.Extension;
    public string Kind => Model.Kind.ToString();
    public string SkipReason => Model.SkipReason?.Description ?? string.Empty;
    public long? SizeInBytes => Model.SizeInBytes;
    public bool IsFallbackText => Model.IsFallbackText;
    public bool IsMergeCandidate => Model.IsMergeCandidate;
    public bool CanOverrideInclusion => IsMergeCandidate;

    public bool AutomaticIncluded { get; }
    public bool AppliedIncluded { get; }

    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (!CanOverrideInclusion)
                return;

            if (!SetProperty(ref _isIncluded, value))
                return;

            OnPropertyChanged(nameof(IsNotIncluded));
            OnPropertyChanged(nameof(HasSkippedStatus));
            OnPropertyChanged(nameof(InclusionBadgeTooltip));
            OnPropertyChanged(nameof(SkippedBadgeTooltip));
            OnPropertyChanged(nameof(FileStateSummaryTooltip));
            OnPropertyChanged(nameof(FileStateSortKey));
        }
    }

    public bool HasManualOverride
    {
        get => _hasManualOverride;
        set
        {
            if (!CanOverrideInclusion && value)
                return;

            if (!SetProperty(ref _hasManualOverride, value))
                return;

            OnPropertyChanged(nameof(HasOverrideStatus));
            OnPropertyChanged(nameof(OverrideBadgeTooltip));
            OnPropertyChanged(nameof(FileStateSummaryTooltip));
            OnPropertyChanged(nameof(FileStateSortKey));
        }
    }

    public bool IsAppliedInPreview
    {
        get => _isAppliedInPreview;
        set
        {
            if (!SetProperty(ref _isAppliedInPreview, value))
                return;

            OnPropertyChanged(nameof(HasPendingPreviewState));
            OnPropertyChanged(nameof(PendingBadgeTooltip));
            OnPropertyChanged(nameof(FileStateSummaryTooltip));
            OnPropertyChanged(nameof(FileStateSortKey));
        }
    }

    public bool IsNotIncluded => !IsIncluded;

    public bool HasOverrideStatus => HasManualOverride;

    public bool HasPendingPreviewState => !IsAppliedInPreview;

    public bool HasFallbackStatus => IsFallbackText;

    public bool HasSkipReason => !string.IsNullOrWhiteSpace(SkipReasonCode);

    public bool HasNoSkipReason => !HasSkipReason;

    public bool HasSkippedStatus => !IsIncluded && HasSkipReason;

    public string SkipReasonCode => Model.SkipReason?.Code ?? string.Empty;

    public string InclusionBadgeTooltip =>
        IsIncluded
            ? "Included in output."
            : "Not included in output.";

    public static string OverrideBadgeTooltip =>
        "Manual inclusion override.";

    public static string PendingBadgeTooltip =>
        "Current file state is not applied to preview yet. Rebuild preview to apply it.";

    public static string FallbackBadgeTooltip =>
        "Unsupported extension included as text fallback.";

    public string SkippedBadgeTooltip =>
        string.IsNullOrWhiteSpace(SkipReason)
            ? "Skipped with reason."
            : $"Skipped with reason: {SkipReason}";

    public string SkipReasonTooltip => BuildSkipReasonTooltip();

    public string FileStateSummaryTooltip => BuildFileStateSummaryTooltip();

    public string FileStateSortKey =>
        $"{(IsIncluded ? 0 : 1)}:" +
        $"{(HasManualOverride ? 1 : 0)}:" +
        $"{(IsAppliedInPreview ? 0 : 1)}:" +
        $"{(IsFallbackText ? 1 : 0)}:" +
        SkipReasonCode;

    public void ResetOverride()
    {
        IsIncluded = AutomaticIncluded;
        HasManualOverride = false;
    }

    public FileInclusionOverride ToOverride()
    {
        if (!CanOverrideInclusion)
        {
            throw new InvalidOperationException(
                "File is not eligible for manual inclusion override.");
        }

        return new FileInclusionOverride(FullPath, IsIncluded);
    }

    private string BuildFileStateSummaryTooltip()
    {
        List<string> lines =
        [
            IsIncluded
                ? "Included in output."
                : "Not included in output."
        ];

        if (HasManualOverride)
            lines.Add("Manual inclusion override.");

        if (!IsAppliedInPreview)
            lines.Add("Current state is not applied to preview yet.");

        if (IsFallbackText)
            lines.Add("Unsupported extension included as text fallback.");

        if (HasSkippedStatus)
            lines.Add(string.IsNullOrWhiteSpace(SkipReason)
                ? "Skipped with reason."
                : $"Skipped with reason: {SkipReason}");

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildSkipReasonTooltip()
    {
        if (Model.SkipReason is null)
            return "No skip reason.";

        StringBuilder builder = new();

        builder.AppendLine(Model.SkipReason.Code);
        builder.AppendLine(Model.SkipReason.Description);

        if (Model.SkipReason.RuleDetails is not null)
        {
            builder.AppendLine();
            builder.AppendLine("Rule:");
            builder.AppendLine($"Mode: {Model.SkipReason.RuleDetails.Mode}");
            builder.AppendLine($"Target: {Model.SkipReason.RuleDetails.Target}");
            builder.AppendLine($"Pattern type: {Model.SkipReason.RuleDetails.PatternType}");
            builder.AppendLine($"Pattern: {Model.SkipReason.RuleDetails.Pattern}");

            if (!string.IsNullOrWhiteSpace(Model.SkipReason.RuleDetails.Description))
                builder.AppendLine($"Description: {Model.SkipReason.RuleDetails.Description}");
        }

        return builder.ToString().TrimEnd();
    }
}