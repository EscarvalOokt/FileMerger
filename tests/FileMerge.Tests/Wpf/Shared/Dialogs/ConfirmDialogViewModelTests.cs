using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Shared.Dialogs;

public sealed class ConfirmDialogViewModelTests
{
    [Fact]
    public void Constructor_Should_Store_Title_Message_And_Button_Labels()
    {
        ConfirmDialogViewModel viewModel = new("Delete profile", "Delete profile 'User Profile'?", "Delete", "Cancel");

        Assert.Equal("Delete profile", viewModel.Title);
        Assert.Equal("Delete profile 'User Profile'?", viewModel.Message);
        Assert.Equal("Delete", viewModel.ConfirmButtonText);
        Assert.Equal("Cancel", viewModel.CancelButtonText);
    }
}