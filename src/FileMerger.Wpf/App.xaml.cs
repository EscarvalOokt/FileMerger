using System.Text;
using System.Windows;
using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.Updates;
using FileMerger.Wpf.Bootstrap;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Shell.Main;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf;

public partial class App
{
    private CrashDiagnosticsService? _crashDiagnostics;

    public static IServiceProvider Services { get; private set; } = null!;

    // ReSharper disable once AsyncVoidEventHandlerMethod
    protected override async void OnStartup(StartupEventArgs e)
    {
        _crashDiagnostics = CrashDiagnosticsService.CreateDefault();
        _crashDiagnostics.Register(this);

        try
        {
            base.OnStartup(e);

            Services = ServiceConfigurator.Configure();

            IUpdateRestartVerificationService restartVerificationService =
                Services.GetRequiredService<IUpdateRestartVerificationService>();
            UpdateRestartVerificationOutcome verificationOutcome =
                await restartVerificationService.PrepareStartupVerificationAsync(e.Args);

            if (verificationOutcome == UpdateRestartVerificationOutcome.Rejected)
            {
                Shutdown(1);
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IApplicationPreferencesStore preferencesStore = Services.GetRequiredService<IApplicationPreferencesStore>();

            await preferencesStore.InitializeAsync();

            MainViewModel mainViewModel = Services.GetRequiredService<MainViewModel>();
            await mainViewModel.InitializeAsync();

            MainWindow window = new()
            {
                DataContext = mainViewModel
            };

            window.Show();

            if (verificationOutcome == UpdateRestartVerificationOutcome.Pending)
            {
                UpdateRestartVerificationOutcome completionOutcome =
                    await restartVerificationService.CompleteStartupVerificationAsync(e.Args);

                if (completionOutcome != UpdateRestartVerificationOutcome.Accepted)
                {
                    Shutdown(1);
                }
            }
        }
        catch (Exception ex)
        {
            _crashDiagnostics.WriteCrashLog(ex, "Startup");
            throw;
        }
    }
}