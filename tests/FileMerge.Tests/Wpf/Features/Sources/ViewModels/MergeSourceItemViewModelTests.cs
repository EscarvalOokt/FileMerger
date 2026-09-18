using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Sources.ViewModels;

namespace FileMerger.Tests.Wpf.Features.Sources.ViewModels;

public sealed class MergeSourceItemViewModelTests
{
    [Fact]
    public void ExclusionSummary_Should_Show_NoExclusions_When_Source_Has_No_Exclusions()
    {
        MergeSourceItemViewModel source = CreateSource();

        Assert.Equal(0, source.ExclusionCount);
        Assert.Equal(0, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.False(source.HasDisabledExclusions);
        Assert.Equal("No exclusions", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Show_Singular_For_One_Enabled_Exclusion()
    {
        MergeSourceItemViewModel source = CreateSource(CreateExclusion("bin", isEnabled: true));

        Assert.Equal(1, source.ExclusionCount);
        Assert.Equal(1, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.False(source.HasDisabledExclusions);
        Assert.Equal("1 exclusion", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Show_Disabled_Singular_For_One_Disabled_Exclusion()
    {
        MergeSourceItemViewModel source = CreateSource(CreateExclusion("bin", isEnabled: false));

        Assert.Equal(1, source.ExclusionCount);
        Assert.Equal(0, source.EnabledExclusionCount);
        Assert.Equal(1, source.DisabledExclusionCount);
        Assert.True(source.HasDisabledExclusions);
        Assert.Equal("1 exclusion · disabled", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Show_Count_For_All_Enabled_Exclusions()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("bin", isEnabled: true),
            CreateExclusion("obj", isEnabled: true),
            CreateExclusion("docs", isEnabled: true));

        Assert.Equal(3, source.ExclusionCount);
        Assert.Equal(3, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.False(source.HasDisabledExclusions);
        Assert.Equal("3 exclusions", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Show_AllDisabled_When_All_Exclusions_Are_Disabled()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("bin", isEnabled: false),
            CreateExclusion("obj", isEnabled: false),
            CreateExclusion("docs", isEnabled: false));

        Assert.Equal(3, source.ExclusionCount);
        Assert.Equal(0, source.EnabledExclusionCount);
        Assert.Equal(3, source.DisabledExclusionCount);
        Assert.True(source.HasDisabledExclusions);
        Assert.Equal("3 exclusions · all disabled", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Show_Enabled_And_Disabled_Counts_When_Mixed()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("enabled-01", isEnabled: true),
            CreateExclusion("enabled-02", isEnabled: true),
            CreateExclusion("enabled-03", isEnabled: true),
            CreateExclusion("disabled-01", isEnabled: false),
            CreateExclusion("disabled-02", isEnabled: false));

        Assert.Equal(5, source.ExclusionCount);
        Assert.Equal(3, source.EnabledExclusionCount);
        Assert.Equal(2, source.DisabledExclusionCount);
        Assert.True(source.HasDisabledExclusions);
        Assert.Equal("5 exclusions · 3 enabled · 2 disabled", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Update_When_Exclusion_Is_Disabled()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("bin", isEnabled: true),
            CreateExclusion("obj", isEnabled: true));

        source.Exclusions[0].IsEnabled = false;

        Assert.Equal(2, source.ExclusionCount);
        Assert.Equal(1, source.EnabledExclusionCount);
        Assert.Equal(1, source.DisabledExclusionCount);
        Assert.True(source.HasDisabledExclusions);
        Assert.Equal("2 exclusions · 1 enabled · 1 disabled", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Update_When_Exclusion_Is_Added()
    {
        MergeSourceItemViewModel source = CreateSource();

        source.Exclusions.Add(CreateExclusion("bin", isEnabled: true));

        Assert.Equal(1, source.ExclusionCount);
        Assert.Equal(1, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.Equal("1 exclusion", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Update_When_Exclusion_Is_Removed()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("bin", isEnabled: true),
            CreateExclusion("obj", isEnabled: false));

        source.Exclusions.RemoveAt(1);

        Assert.Equal(1, source.ExclusionCount);
        Assert.Equal(1, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.False(source.HasDisabledExclusions);
        Assert.Equal("1 exclusion", source.ExclusionSummary);
    }

    [Fact]
    public void ExclusionSummary_Should_Update_When_Exclusions_Are_Cleared()
    {
        MergeSourceItemViewModel source = CreateSource(
            CreateExclusion("bin", isEnabled: true),
            CreateExclusion("obj", isEnabled: false));

        source.Exclusions.Clear();

        Assert.Equal(0, source.ExclusionCount);
        Assert.Equal(0, source.EnabledExclusionCount);
        Assert.Equal(0, source.DisabledExclusionCount);
        Assert.False(source.HasDisabledExclusions);
        Assert.Equal("No exclusions", source.ExclusionSummary);
    }

    private static MergeSourceItemViewModel CreateSource(params MergeSourceExclusionItemViewModel[] exclusions)
    {
        var source = new MergeSourceItemViewModel(
            new MergeSource(path: @"D:\Project", type: MergeSourceType.Directory, isRecursive: true, isEnabled: true));

        foreach (MergeSourceExclusionItemViewModel exclusion in exclusions)
            source.Exclusions.Add(exclusion);

        return source;
    }

    private static MergeSourceExclusionItemViewModel CreateExclusion(string relativePath, bool isEnabled)
    {
        return new MergeSourceExclusionItemViewModel(
            new MergeSourceExclusion(
                relativePath: relativePath,
                type: MergeSourceExclusionType.Directory,
                isEnabled: isEnabled));
    }
}