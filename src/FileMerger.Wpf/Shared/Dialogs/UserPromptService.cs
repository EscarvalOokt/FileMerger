using System.Windows;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Shared.Dialogs;

public sealed class UserPromptService : IUserPromptService
{
    private readonly IWindowOwnerResolver _ownerResolver;

    public UserPromptService(IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(ownerResolver);
        _ownerResolver = ownerResolver;
    }

    public UnsavedChangesDecision ConfirmUnsavedChanges(string title, string message)
    {
        UnsavedChangesDialogViewModel viewModel = new(title, message);
        UnsavedChangesDialogWindow window = new()
        {
            DataContext = viewModel
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        bool? result = window.ShowDialog();
        if (result != true)
            return UnsavedChangesDecision.Cancel;

        return viewModel.Decision;
    }

    public bool Confirm(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "No")
    {
        ConfirmDialogViewModel viewModel = new(title, message, confirmButtonText, cancelButtonText);
        ConfirmDialogWindow window = new()
        {
            DataContext = viewModel
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        return window.ShowDialog() == true;
    }
}