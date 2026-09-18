using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;

public sealed class WorkspaceTabRenameViewModel : ViewModelBase
{
    private string _workspaceName;

    public WorkspaceTabRenameViewModel(string currentName)
    {
        OriginalName = string.IsNullOrWhiteSpace(currentName) ? "Untitled Workspace" : currentName.Trim();

        _workspaceName = OriginalName;
    }

    public string OriginalName { get; }

    public string WorkspaceName
    {
        get => _workspaceName;
        set
        {
            if (SetProperty(ref _workspaceName, value))
            {
                OnPropertyChanged(nameof(NormalizedName));
                OnPropertyChanged(nameof(CanConfirm));
                OnPropertyChanged(nameof(ValidationMessage));
            }
        }
    }

    public string NormalizedName => WorkspaceName.Trim();

    public bool CanConfirm => !string.IsNullOrWhiteSpace(WorkspaceName);

    public string ValidationMessage => CanConfirm ? string.Empty : "Workspace tab name cannot be empty.";
}