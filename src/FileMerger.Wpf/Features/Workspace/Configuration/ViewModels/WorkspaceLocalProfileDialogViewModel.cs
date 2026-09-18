using System.ComponentModel;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Shared.Commands;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Configuration.ViewModels;

public sealed class WorkspaceLocalProfileDialogViewModel : ViewModelBase
{
    public WorkspaceLocalProfileDialogViewModel(
        string profileName,
        WorkspaceProfileDto profile,
        IProfileEditorFactory profileEditorFactory)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profileEditorFactory);

        Editor = profileEditorFactory.Create();
        Editor.WorkingProfileName = profileName;
        Editor.ApplyProfile(profile);
        Editor.PropertyChanged += Editor_PropertyChanged;

        OkCommand = new RelayCommand(ApplyAndClose, () => Editor.CanUseProfile);
        CancelCommand = new RelayCommand(Cancel);
    }

    public ProfileEditorViewModel Editor { get; }

    public RelayCommand OkCommand { get; }

    public RelayCommand CancelCommand { get; }

    public WorkspaceLocalProfileEditResult? Result { get; private set; }

    public event EventHandler<bool?>? RequestClose;

    private void Editor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfileEditorViewModel.CanUseProfile))
            OkCommand.RaiseCanExecuteChanged();
    }

    private void ApplyAndClose()
    {
        if (!Editor.CanUseProfile)
            return;

        Result = new WorkspaceLocalProfileEditResult(Editor.WorkingProfileName, Editor.CaptureProfile());
        RequestClose?.Invoke(this, true);
    }

    private void Cancel()
    {
        Result = null;
        RequestClose?.Invoke(this, false);
    }
}