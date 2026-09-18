using FileMerger.Application.Updates;
using FileMerger.Application.UseCases.BuildPreview;
using FileMerger.Application.UseCases.CheckForUpdates;
using FileMerger.Application.UseCases.DownloadAndValidateUpdatePackage;
using FileMerger.Application.UseCases.LaunchUpdateInstaller;
using FileMerger.Application.UseCases.PrepareUpdateInstallation;
using FileMerger.Application.UseCases.SaveOutput;
using FileMerger.Domain.Profiles;
using FileMerger.Infrastructure.DependencyInjection;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Features.Profile.Services;
using FileMerger.Wpf.Features.Profile.ViewModels;
using FileMerger.Wpf.Features.Profile.Views;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Features.Settings.Dialogs;
using FileMerger.Wpf.Features.Settings.ViewModels;
using FileMerger.Wpf.Features.Settings.Views;
using FileMerger.Wpf.Features.Sources.Dialogs;
using FileMerger.Wpf.Features.Updates.Dialogs;
using FileMerger.Wpf.Features.Updates.Services;
using FileMerger.Wpf.Features.Updates.ViewModels;
using FileMerger.Wpf.Features.Updates.Views;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration.Dialogs;
using FileMerger.Wpf.Features.Workspace.Configuration.Views;
using FileMerger.Wpf.Features.Workspace.Recent;
using FileMerger.Wpf.Features.Workspace.Tabs;
using FileMerger.Wpf.Features.Workspace.Tabs.Dialogs;
using FileMerger.Wpf.Shared.Dialogs;
using FileMerger.Wpf.Shared.Integration;
using FileMerger.Wpf.Shell.Help;
using FileMerger.Wpf.Shell.Main;
using FileMerger.Wpf.Shell.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf.Bootstrap;

public static class ServiceConfigurator
{
    public static IServiceProvider Configure()
    {
        ServiceCollection services = new();

        services.AddFileMergerInfrastructure();

        services.AddSingleton<BuildMergePreviewUseCase>();
        services.AddSingleton<SaveMergeOutputUseCase>();
        services.AddSingleton<UpdateReleaseManifestParser>();
        services.AddSingleton<UpdateCompatibilityPolicy>();
        services.AddSingleton<CheckForUpdatesUseCase>();
        services.AddSingleton<DownloadAndValidateUpdatePackageUseCase>();
        services.AddSingleton<PrepareUpdateInstallationUseCase>();
        services.AddSingleton<LaunchUpdateInstallerUseCase>();

        services.AddSingleton<IFileTypeCatalog, BuiltInFileTypeCatalog>();
        services.AddSingleton<IBuiltInProfilePresetProvider, BuiltInProfilePresetProvider>();
        services.AddSingleton<IProfileLibraryService, ProfileLibraryService>();
        services.AddSingleton<IFileDialogFilterBuilder, SupportedInputFileDialogFilterBuilder>();

        services.AddSingleton<ISaveOutputFileTypeCatalog, BuiltInSaveOutputFileTypeCatalog>();
        services.AddSingleton<ISaveOutputDialogFilterBuilder, DefaultSaveOutputDialogFilterBuilder>();

        services.AddSingleton<IWindowOwnerResolver, WindowOwnerResolver>();
        services.AddSingleton<IKeyboardShortcutsDialogService, KeyboardShortcutsDialogService>();
        services.AddSingleton<IUpdateCheckDialogService, UpdateCheckDialogService>();
        services.AddSingleton<IProfileFilterRulesDialogService, ProfileFilterRulesDialogService>();
        services.AddSingleton<IApplicationWindowCloseGuardService, ApplicationWindowCloseGuardService>();

        services.AddSingleton<IFolderBrowserService, FolderBrowserService>();
        services.AddSingleton<ISaveFileDialogService, SaveFileDialogService>();
        services.AddSingleton<IOpenFileDialogService, OpenFileDialogService>();
        services.AddSingleton<IUserPromptService, UserPromptService>();
        services.AddSingleton<ISourceDetailsDialogService, SourceDetailsDialogService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFileSystemLauncher, FileSystemLauncher>();
        services.AddSingleton<IApplicationShutdownService, ApplicationShutdownService>();

        services.AddSingleton<RecentWorkspacesStoragePathPolicy>();
        services.AddSingleton<IRecentWorkspacesService, JsonRecentWorkspacesService>();
        services.AddSingleton<ApplicationPreferencesStoragePathPolicy>();
        services.AddSingleton<IApplicationPreferencesService, JsonApplicationPreferencesService>();
        services.AddSingleton<IApplicationPreferencesStore, ApplicationPreferencesStore>();
        services.AddSingleton<IPreferencesDialogService, PreferencesDialogService>();
        services.AddSingleton<CrashLogPathPolicy>();
        services.AddSingleton<ICrashLogMaintenanceService, CrashLogMaintenanceService>();

        services.AddSingleton<IWorkspacePersistenceService, JsonWorkspacePersistenceService>();
        services.AddSingleton<IWorkspaceCoordinator, WorkspaceCoordinator>();
        services.AddSingleton<IWorkspaceDocumentFactory, WorkspaceDocumentFactory>();
        services.AddSingleton<IWorkspaceDocumentCloneService, WorkspaceDocumentCloneService>();
        services.AddSingleton<IWorkspaceConfigurationDialogService, WorkspaceConfigurationDialogService>();
        services.AddSingleton<IWorkspaceLocalProfileDialogService, WorkspaceLocalProfileDialogService>();
        services.AddSingleton<IWorkspaceTabRenameDialogService, WorkspaceTabRenameDialogService>();
        services.AddSingleton<WorkspaceTabManagerViewModel>();

        services.AddSingleton<IMainStateFactory, MainStateFactory>();
        services.AddSingleton<IWorkspaceDocumentDirtyStateService, WorkspaceDocumentDirtyStateService>();
        services.AddSingleton<IWorkspaceDocumentPreviewService, WorkspaceDocumentPreviewService>();
        services.AddSingleton<IWorkspaceDocumentOutputService, WorkspaceDocumentOutputService>();
        services.AddSingleton<IWorkspaceDocumentLifecycleService, WorkspaceDocumentLifecycleService>();

        services.AddSingleton<IProfileEditorFactory, ProfileEditorFactory>();
        services.AddSingleton<IProfileManagerWindowService, ProfileManagerWindowService>();

        services.AddSingleton<IUpdateInstallationCoordinator, UpdateInstallationCoordinator>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ICurrentSessionProfileHost>(sp => sp.GetRequiredService<MainViewModel>());

        services.AddTransient<ProfileManagerViewModel>();
        services.AddTransient<ProfileManagerWindow>();
        services.AddTransient<PreferencesDialogViewModel>();
        services.AddTransient<PreferencesWindow>();
        services.AddTransient<UpdateCheckDialogViewModel>();
        services.AddTransient<UpdateCheckWindow>();
        services.AddTransient<WorkspaceConfigurationWindow>();
        services.AddTransient<WorkspaceLocalProfileWindow>();

        return services.BuildServiceProvider();
    }
}