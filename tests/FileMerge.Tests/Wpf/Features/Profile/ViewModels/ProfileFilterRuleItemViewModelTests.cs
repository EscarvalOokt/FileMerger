using FileMerger.Domain.Enums;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace;

namespace FileMerger.Tests.Wpf.Features.Profile.ViewModels;

public sealed class ProfileFilterRuleItemViewModelTests
{
    [Fact]
    public void FromDto_Should_Map_All_Fields()
    {
        var dto = new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: "Library",
            IsEnabled: false,
            Description: "Exclude Library",
            IsUserEditable: true);

        var vm = ProfileFilterRuleItemViewModel.FromDto(dto);

        Assert.Equal(dto.Mode, vm.Mode);
        Assert.Equal(dto.Target, vm.Target);
        Assert.Equal(dto.PatternType, vm.PatternType);
        Assert.Equal(dto.Pattern, vm.Pattern);
        Assert.Equal(dto.IsEnabled, vm.IsEnabled);
        Assert.Equal(dto.Description, vm.Description);
        Assert.Equal(dto.IsUserEditable, vm.IsUserEditable);
    }

    [Fact]
    public void ToDto_Should_Map_All_Fields()
    {
        var vm = new ProfileFilterRuleItemViewModel(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Include,
            Target: FilterTarget.Extension,
            PatternType: RulePatternType.Exact,
            Pattern: ".cs",
            IsEnabled: true,
            Description: "Include C#",
            IsUserEditable: true));

        WorkspaceFileFilterRuleDto dto = vm.ToDto();

        Assert.Equal(FilterMode.Include, dto.Mode);
        Assert.Equal(FilterTarget.Extension, dto.Target);
        Assert.Equal(RulePatternType.Exact, dto.PatternType);
        Assert.Equal(".cs", dto.Pattern);
        Assert.True(dto.IsEnabled);
        Assert.Equal("Include C#", dto.Description);
        Assert.True(dto.IsUserEditable);
    }

    [Fact]
    public void Pattern_Should_Be_Invalid_When_Empty()
    {
        var vm = new ProfileFilterRuleItemViewModel
        {
            Pattern = ""
        };

        Assert.True(vm.HasValidationError);
        Assert.Equal("Pattern cannot be empty.", vm.ValidationMessage);
    }

    [Fact]
    public void Regex_Should_Be_Invalid_When_Pattern_Is_Invalid()
    {
        var vm = new ProfileFilterRuleItemViewModel(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.FileName,
            PatternType: RulePatternType.Regex,
            Pattern: "[",
            IsEnabled: true));

        Assert.True(vm.HasValidationError);
        Assert.Contains("Invalid regex:", vm.ValidationMessage);
    }

    [Fact]
    public void DirectorySegment_Should_Be_Invalid_When_Pattern_Contains_Path_Separator()
    {
        var vm = new ProfileFilterRuleItemViewModel(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.DirectorySegment,
            PatternType: RulePatternType.Exact,
            Pattern: @"Library\PackageCache",
            IsEnabled: true));

        Assert.True(vm.HasValidationError);
        Assert.Equal("Directory segment cannot contain path separators.", vm.ValidationMessage);
    }

    [Fact]
    public void Extension_Should_Be_Invalid_When_Pattern_Does_Not_Start_With_Dot()
    {
        var vm = new ProfileFilterRuleItemViewModel(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Include,
            Target: FilterTarget.Extension,
            PatternType: RulePatternType.Exact,
            Pattern: "cs",
            IsEnabled: true));

        Assert.True(vm.HasValidationError);
        Assert.Equal("Extension pattern should start with '.'.", vm.ValidationMessage);
    }

    [Fact]
    public void Clone_Should_Copy_Current_Rule()
    {
        var vm = new ProfileFilterRuleItemViewModel(new WorkspaceFileFilterRuleDto(
            Mode: FilterMode.Exclude,
            Target: FilterTarget.FileName,
            PatternType: RulePatternType.Wildcard,
            Pattern: "*.g.cs",
            IsEnabled: true,
            Description: "Generated",
            IsUserEditable: true));

        ProfileFilterRuleItemViewModel clone = vm.Clone();

        Assert.NotSame(vm, clone);
        Assert.Equal(vm.Mode, clone.Mode);
        Assert.Equal(vm.Target, clone.Target);
        Assert.Equal(vm.PatternType, clone.PatternType);
        Assert.Equal(vm.Pattern, clone.Pattern);
        Assert.Equal(vm.IsEnabled, clone.IsEnabled);
        Assert.Equal(vm.Description, clone.Description);
        Assert.Equal(vm.IsUserEditable, clone.IsUserEditable);
    }
}