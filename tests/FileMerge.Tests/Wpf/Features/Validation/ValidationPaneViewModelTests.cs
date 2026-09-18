using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Features.Validation.ViewModels;
using FileMerger.Wpf.Shared.Status;

namespace FileMerger.Tests.Wpf.Features.Validation;

public sealed class ValidationPaneViewModelTests
{
    [Fact]
    public void Constructor_Should_Start_With_Compact_Empty_State()
    {
        ValidationPaneViewModel vm = new();

        Assert.Empty(vm.Issues);
        Assert.Equal(0, vm.TotalCount);
        Assert.False(vm.HasIssues);
        Assert.False(vm.HasErrors);
        Assert.False(vm.HasWarnings);
        Assert.False(vm.HasInfo);
        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.None, vm.HighestStatusSeverity);
        Assert.Equal("No validation issues.", vm.SummaryText);
        Assert.Equal("Errors 0 · Warnings 0 · Info 0", vm.CounterSummaryText);
        Assert.Equal("Show details", vm.DetailsToggleText);
    }

    [Fact]
    public void Clear_Should_Collapse_Details_And_Reset_Summary()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Error, "error.code")
        ]);

        Assert.True(vm.IsDetailsExpanded);

        vm.Clear();

        Assert.Empty(vm.Issues);
        Assert.Equal(0, vm.TotalCount);
        Assert.False(vm.HasIssues);
        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.None, vm.HighestStatusSeverity);
        Assert.Equal("No validation issues.", vm.SummaryText);
        Assert.Equal("Errors 0 · Warnings 0 · Info 0", vm.CounterSummaryText);
    }

    [Fact]
    public void Load_Should_Collapse_Details_When_Issues_Are_Empty()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Warning, "warning.code")
        ]);

        Assert.True(vm.IsDetailsExpanded);

        vm.Load([]);

        Assert.Empty(vm.Issues);
        Assert.False(vm.HasIssues);
        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal("No validation issues.", vm.SummaryText);
        Assert.Equal("Errors 0 · Warnings 0 · Info 0", vm.CounterSummaryText);
    }

    [Fact]
    public void Load_Should_Keep_Info_Only_Details_Collapsed_By_Default()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Info, "info.code")
        ]);

        Assert.Single(vm.Issues);
        Assert.Equal(1, vm.TotalCount);
        Assert.True(vm.HasIssues);
        Assert.True(vm.HasInfo);
        Assert.False(vm.HasWarnings);
        Assert.False(vm.HasErrors);
        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Info, vm.HighestStatusSeverity);
        Assert.Equal("1 info", vm.SummaryText);
        Assert.Equal("Errors 0 · Warnings 0 · Info 1", vm.CounterSummaryText);
    }

    [Fact]
    public void Load_Should_Preserve_Manual_Info_Only_Expansion()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Info, "info.code")
        ]);

        vm.IsDetailsExpanded = true;

        vm.Load(
        [
            Issue(ValidationSeverity.Info, "another.info.code")
        ]);

        Assert.True(vm.IsDetailsExpanded);
        Assert.True(vm.HasVisibleDetails);
        Assert.Equal("Hide details", vm.DetailsToggleText);
    }

    [Fact]
    public void Load_Should_Expand_Details_When_Warning_Exists()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Warning, "warning.code")
        ]);

        Assert.Single(vm.Issues);
        Assert.Equal(1, vm.TotalCount);
        Assert.True(vm.HasIssues);
        Assert.True(vm.HasWarnings);
        Assert.False(vm.HasErrors);
        Assert.True(vm.IsDetailsExpanded);
        Assert.True(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Warning, vm.HighestStatusSeverity);
        Assert.Equal("1 warning(s)", vm.SummaryText);
        Assert.Equal("Errors 0 · Warnings 1 · Info 0", vm.CounterSummaryText);
        Assert.Equal("Hide details", vm.DetailsToggleText);
    }

    [Fact]
    public void Load_Should_Expand_Details_When_Error_Exists()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Error, "error.code")
        ]);

        Assert.Single(vm.Issues);
        Assert.Equal(1, vm.TotalCount);
        Assert.True(vm.HasIssues);
        Assert.True(vm.HasErrors);
        Assert.False(vm.HasWarnings);
        Assert.True(vm.IsDetailsExpanded);
        Assert.True(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Error, vm.HighestStatusSeverity);
        Assert.Equal("1 error(s)", vm.SummaryText);
        Assert.Equal("Errors 1 · Warnings 0 · Info 0", vm.CounterSummaryText);
        Assert.Equal("Hide details", vm.DetailsToggleText);
    }

    [Fact]
    public void Load_Should_Count_All_Severity_Types()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Error, "error.code"),
            Issue(ValidationSeverity.Warning, "warning.code"),
            Issue(ValidationSeverity.Info, "info.code")
        ]);

        Assert.Equal(3, vm.TotalCount);
        Assert.Equal(1, vm.ErrorCount);
        Assert.Equal(1, vm.WarningCount);
        Assert.Equal(1, vm.InfoCount);
        Assert.True(vm.HasErrors);
        Assert.True(vm.HasWarnings);
        Assert.True(vm.HasInfo);
        Assert.True(vm.IsDetailsExpanded);
        Assert.True(vm.HasVisibleDetails);
        Assert.Equal(StatusSeverity.Error, vm.HighestStatusSeverity);
        Assert.Equal("1 error(s) · 1 warning(s) · 1 info", vm.SummaryText);
        Assert.Equal("Errors 1 · Warnings 1 · Info 1", vm.CounterSummaryText);
    }

    [Fact]
    public void IsDetailsExpanded_Should_Update_Dependent_State()
    {
        ValidationPaneViewModel vm = new();

        vm.Load(
        [
            Issue(ValidationSeverity.Info, "info.code")
        ]);

        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal("Show details", vm.DetailsToggleText);

        vm.IsDetailsExpanded = true;

        Assert.True(vm.IsDetailsExpanded);
        Assert.True(vm.HasVisibleDetails);
        Assert.Equal("Hide details", vm.DetailsToggleText);

        vm.IsDetailsExpanded = false;

        Assert.False(vm.IsDetailsExpanded);
        Assert.False(vm.HasVisibleDetails);
        Assert.Equal("Show details", vm.DetailsToggleText);
    }

    [Fact]
    public void Load_Should_Throw_When_Issues_Are_Null()
    {
        ValidationPaneViewModel vm = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => vm.Load(null!));

        Assert.Equal("issues", ex.ParamName);
    }

    private static ValidationIssue Issue(ValidationSeverity severity, string code)
    {
        return new ValidationIssue(severity, code, $"{code} message");
    }
}