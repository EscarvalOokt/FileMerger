using System.Windows;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Profile.Views;
using FileMerger.Wpf.Shell.Windows;

namespace FileMerger.Wpf.Features.Profile.Services;

public sealed class ProfileFilterRulesDialogService : IProfileFilterRulesDialogService
{
    private readonly IWindowOwnerResolver _ownerResolver;

    public ProfileFilterRulesDialogService(IWindowOwnerResolver ownerResolver)
    {
        ArgumentNullException.ThrowIfNull(ownerResolver);
        _ownerResolver = ownerResolver;
    }

    public void Show(ProfileEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        ProfileFilterRulesDialogWindow window = new()
        {
            DataContext = editor
        };

        Window? owner = _ownerResolver.ResolveOwner(window);
        if (owner is not null)
            window.Owner = owner;

        window.ShowDialog();
    }
}