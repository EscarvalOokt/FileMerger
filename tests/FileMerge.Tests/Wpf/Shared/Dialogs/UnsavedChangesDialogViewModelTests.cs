using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Shared.Dialogs;

public sealed class UnsavedChangesDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Store_Title_And_Message()
    {
        UnsavedChangesDialogViewModel viewModel = new(
            "Unsaved workspace",
            "Workspace has unsaved changes.");

        Assert.Equal("Unsaved workspace", viewModel.Title);
        Assert.Equal("Workspace has unsaved changes.", viewModel.Message);
    }

    [Fact]
    public void Constructor_Should_Default_Decision_To_Cancel()
    {
        UnsavedChangesDialogViewModel viewModel = new(
            "Unsaved workspace",
            "Workspace has unsaved changes.");

        Assert.Equal(UnsavedChangesDecision.Cancel, viewModel.Decision);
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Save)]
    [InlineData(UnsavedChangesDecision.Discard)]
    [InlineData(UnsavedChangesDecision.Cancel)]
    public void Choose_Should_Set_Decision(UnsavedChangesDecision decision)
    {
        UnsavedChangesDialogViewModel viewModel = new(
            "Unsaved workspace",
            "Workspace has unsaved changes.");

        viewModel.Choose(decision);

        Assert.Equal(decision, viewModel.Decision);
    }
}