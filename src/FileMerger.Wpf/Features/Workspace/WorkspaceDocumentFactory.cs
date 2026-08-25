using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Session.ViewModels;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Sources.Dialogs;
using FileMerger.Wpf.Features.Sources.ViewModels;
using FileMerger.Wpf.Features.Validation.ViewModels;
using FileMerger.Wpf.Features.Workspace.State;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Integration;

namespace FileMerger.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentFactory : IWorkspaceDocumentFactory
{
    private readonly IProfileEditorFactory _profileEditorFactory;
    private readonly IFolderBrowserService _folderBrowserService;
    private readonly IOpenFileDialogService _openFileDialogService;
    private readonly ISourceDetailsDialogService _sourceDetailsDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly IApplicationPreferencesStore _applicationPreferencesStore;

    public WorkspaceDocumentFactory(
        IProfileEditorFactory profileEditorFactory,
        IFolderBrowserService folderBrowserService,
        IOpenFileDialogService openFileDialogService,
        ISourceDetailsDialogService sourceDetailsDialogService,
        IClipboardService clipboardService,
        IApplicationPreferencesStore applicationPreferencesStore)
    {
        ArgumentNullException.ThrowIfNull(profileEditorFactory);
        ArgumentNullException.ThrowIfNull(folderBrowserService);
        ArgumentNullException.ThrowIfNull(openFileDialogService);
        ArgumentNullException.ThrowIfNull(sourceDetailsDialogService);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(applicationPreferencesStore);

        _profileEditorFactory = profileEditorFactory;
        _folderBrowserService = folderBrowserService;
        _openFileDialogService = openFileDialogService;
        _sourceDetailsDialogService = sourceDetailsDialogService;
        _clipboardService = clipboardService;
        _applicationPreferencesStore = applicationPreferencesStore;
    }

    public WorkspaceDocumentViewModel CreateDefaultDocument()
    {
        SessionSettingsViewModel sessionSettings = new();
        sessionSettings.LoadDefaults();

        ProfileEditorViewModel profileEditor = _profileEditorFactory.Create();

        SourcesPaneViewModel sourcesPane = new(
            _folderBrowserService,
            _openFileDialogService,
            _sourceDetailsDialogService);

        FilesPaneViewModel filesPane = new(_clipboardService);
        ValidationPaneViewModel validationPane = new();
        PreviewDirtyStateTracker previewDirtyTracker = new();
        WorkspaceDirtyStateTracker workspaceDirtyTracker = new();
        AppliedPreviewFileStateStore appliedPreviewFileStateStore = new();

        previewDirtyTracker.Reset();

        WorkspaceDocumentViewModel document = new(
            sessionSettings,
            profileEditor,
            sourcesPane,
            filesPane,
            validationPane,
            previewDirtyTracker,
            workspaceDirtyTracker,
            appliedPreviewFileStateStore)
        {
            IsPreviewLineWrapEnabled =
                _applicationPreferencesStore.Current.IsPreviewLineWrapEnabledByDefault
        };

        workspaceDirtyTracker.MarkWorkspaceSaved(
            WorkspaceDocumentStateSnapshotFactory.Capture(document));

        return document;
    }
}