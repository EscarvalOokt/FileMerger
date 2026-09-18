using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Files.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Files.ViewModels;

public sealed class InputFileItemViewModelTests
{
    [Fact]
    public void StateProperties_Should_Describe_Included_File()
    {
        InputFileItemViewModel item = CreateItem("Included.cs");

        Assert.True(item.IsIncluded);
        Assert.False(item.IsNotIncluded);
        Assert.False(item.HasSkippedStatus);
        Assert.Equal("Included in output.", item.InclusionBadgeTooltip);
        Assert.Contains("Included in output.", item.FileStateSummaryTooltip);
    }

    [Fact]
    public void StateProperties_Should_Describe_NotIncluded_File()
    {
        InputFileItemViewModel item = CreateItem("Excluded.cs", currentIncluded: false);

        Assert.False(item.IsIncluded);
        Assert.True(item.IsNotIncluded);
        Assert.Equal("Not included in output.", item.InclusionBadgeTooltip);
        Assert.Contains("Not included in output.", item.FileStateSummaryTooltip);
    }

    [Fact]
    public void OverrideProperties_Should_Be_Visible_When_File_Has_ManualOverride()
    {
        InputFileItemViewModel item = CreateItem("Override.cs", automaticIncluded: true, currentIncluded: false);

        Assert.True(item.HasManualOverride);
        Assert.True(item.HasOverrideStatus);
        Assert.Equal("Manual inclusion override.", InputFileItemViewModel.OverrideBadgeTooltip);
        Assert.Contains("Manual inclusion override.", item.FileStateSummaryTooltip);
    }

    [Fact]
    public void PendingProperties_Should_Be_Visible_When_File_Is_NotAppliedInPreview()
    {
        InputFileItemViewModel item = CreateItem("Pending.cs", currentIncluded: false, appliedIncluded: true);

        Assert.False(item.IsAppliedInPreview);
        Assert.True(item.HasPendingPreviewState);
        Assert.Contains(
            "not applied to preview",
            InputFileItemViewModel.PendingBadgeTooltip,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not applied to preview", item.FileStateSummaryTooltip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FallbackProperties_Should_Be_Visible_When_File_Is_FallbackText()
    {
        InputFileItemViewModel item = CreateItem(
            "Unknown.custom",
            extension: ".custom",
            kind: FileKind.Text,
            isFallbackText: true);

        Assert.True(item.IsFallbackText);
        Assert.True(item.HasFallbackStatus);
        Assert.Equal("Unsupported extension included as text fallback.", InputFileItemViewModel.FallbackBadgeTooltip);
        Assert.Contains("text fallback", item.FileStateSummaryTooltip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SkipProperties_Should_Use_SkipReason_Code_And_Description()
    {
        var reason = new SkipReason("filter.rule.exclude", "Excluded by rule.");

        InputFileItemViewModel item = CreateItem(
            "Generated.g.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: reason);

        Assert.True(item.HasSkipReason);
        Assert.False(item.HasNoSkipReason);
        Assert.True(item.HasSkippedStatus);
        Assert.Equal("filter.rule.exclude", item.SkipReasonCode);
        Assert.Contains("filter.rule.exclude", item.SkipReasonTooltip);
        Assert.Contains("Excluded by rule.", item.SkipReasonTooltip);
        Assert.Contains("Skipped with reason", item.SkippedBadgeTooltip);
    }

    [Fact]
    public void SkipReasonTooltip_Should_Include_RuleDetails_When_Available()
    {
        var reason = new SkipReason(
            "filter.rule.exclude",
            "Excluded by rule.",
            new SkipRuleDetails(
                FilterMode.Exclude,
                FilterTarget.FileName,
                RulePatternType.Wildcard,
                "*.Designer.cs",
                "Generated designer files"));

        InputFileItemViewModel item = CreateItem(
            "Form.Designer.cs",
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: reason);

        Assert.Contains("Rule:", item.SkipReasonTooltip);
        Assert.Contains("Mode: Exclude", item.SkipReasonTooltip);
        Assert.Contains("Target: FileName", item.SkipReasonTooltip);
        Assert.Contains("Pattern type: Wildcard", item.SkipReasonTooltip);
        Assert.Contains("Pattern: *.Designer.cs", item.SkipReasonTooltip);
        Assert.Contains("Generated designer files", item.SkipReasonTooltip);
    }

    [Fact]
    public void SkipProperties_Should_Show_NoReason_State_When_SkipReason_Is_Missing()
    {
        InputFileItemViewModel item = CreateItem("Regular.cs");

        Assert.False(item.HasSkipReason);
        Assert.True(item.HasNoSkipReason);
        Assert.Equal(string.Empty, item.SkipReasonCode);
        Assert.Equal("No skip reason.", item.SkipReasonTooltip);
    }

    [Fact]
    public void FileStateSortKey_Should_Change_When_Inclusion_Changes()
    {
        InputFileItemViewModel item = CreateItem("File.cs");

        string before = item.FileStateSortKey;

        item.IsIncluded = false;

        Assert.NotEqual(before, item.FileStateSortKey);
    }

    [Fact]
    public void FileStateSortKey_Should_Change_When_Override_Changes()
    {
        InputFileItemViewModel item = CreateItem("File.cs");

        string before = item.FileStateSortKey;

        item.HasManualOverride = true;

        Assert.NotEqual(before, item.FileStateSortKey);
    }

    [Fact]
    public void FileStateSortKey_Should_Change_When_PendingPreviewState_Changes()
    {
        InputFileItemViewModel item = CreateItem("File.cs");

        string before = item.FileStateSortKey;

        item.IsAppliedInPreview = false;

        Assert.NotEqual(before, item.FileStateSortKey);
    }

    [Fact]
    public void Changing_IsIncluded_Should_Raise_Dependent_State_PropertyChanges()
    {
        InputFileItemViewModel item = CreateItem("File.cs");
        List<string?> propertyNames = [];

        item.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        item.IsIncluded = false;

        Assert.Contains(nameof(InputFileItemViewModel.IsIncluded), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.IsNotIncluded), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.HasSkippedStatus), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.InclusionBadgeTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSummaryTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSortKey), propertyNames);
    }

    [Fact]
    public void Changing_HasManualOverride_Should_Raise_Dependent_State_PropertyChanges()
    {
        InputFileItemViewModel item = CreateItem("File.cs");
        List<string?> propertyNames = [];

        item.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        item.HasManualOverride = true;

        Assert.Contains(nameof(InputFileItemViewModel.HasManualOverride), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.HasOverrideStatus), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.OverrideBadgeTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSummaryTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSortKey), propertyNames);
    }

    [Fact]
    public void Changing_IsAppliedInPreview_Should_Raise_Dependent_State_PropertyChanges()
    {
        InputFileItemViewModel item = CreateItem("File.cs");
        List<string?> propertyNames = [];

        item.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        item.IsAppliedInPreview = false;

        Assert.Contains(nameof(InputFileItemViewModel.IsAppliedInPreview), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.HasPendingPreviewState), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.PendingBadgeTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSummaryTooltip), propertyNames);
        Assert.Contains(nameof(InputFileItemViewModel.FileStateSortKey), propertyNames);
    }


    [Fact]
    public void MergeCandidateProperties_Should_Reflect_Model_Eligibility()
    {
        InputFileItemViewModel candidate = CreateItem("Candidate.cs");
        InputFileItemViewModel nonCandidate = CreateItem(
            "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        Assert.True(candidate.IsMergeCandidate);
        Assert.True(candidate.CanOverrideInclusion);
        Assert.False(nonCandidate.IsMergeCandidate);
        Assert.False(nonCandidate.CanOverrideInclusion);
    }

    [Fact]
    public void IsIncluded_Should_Not_Change_For_NonCandidate_File()
    {
        InputFileItemViewModel item = CreateItem(
            "Unsupported.bin",
            extension: ".bin",
            kind: FileKind.Unknown,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason(
                "discovery.unsupported-file-type",
                "File type is not supported by the current profile."),
            isMergeCandidate: false);

        item.IsIncluded = true;

        Assert.False(item.IsIncluded);
        Assert.False(item.HasManualOverride);
    }

    [Fact]
    public void ToOverride_Should_Throw_For_NonCandidate_File()
    {
        InputFileItemViewModel item = CreateItem(
            "Disabled.json",
            extension: ".json",
            kind: FileKind.Json,
            automaticIncluded: false,
            currentIncluded: false,
            appliedIncluded: false,
            skipReason: new SkipReason("discovery.file-type-disabled", "File type is disabled in the current profile."),
            isMergeCandidate: false);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(item.ToOverride);

        Assert.Contains("not eligible", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static InputFileItemViewModel CreateItem(
        string relativePath,
        string? extension = null,
        FileKind kind = FileKind.CSharp,
        bool automaticIncluded = true,
        bool currentIncluded = true,
        bool appliedIncluded = true,
        SkipReason? skipReason = null,
        bool isFallbackText = false,
        bool isMergeCandidate = true)
    {
        extension ??= Path.GetExtension(relativePath);

        var model = new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: extension,
            kind: kind,
            isIncluded: automaticIncluded,
            skipReason: skipReason,
            isFallbackText: isFallbackText,
            isMergeCandidate: isMergeCandidate);

        return new InputFileItemViewModel(
            model: model,
            automaticIncluded: automaticIncluded,
            currentIncluded: currentIncluded,
            appliedIncluded: appliedIncluded)
        {
            HasManualOverride = currentIncluded != automaticIncluded,
            IsAppliedInPreview = currentIncluded == appliedIncluded
        };
    }
}