using System.Text;
using System.Windows;
using FileMerger.Wpf.Bootstrap;
using FileMerger.Wpf.Diagnostics;
using FileMerger.Wpf.Features.Settings;
using FileMerger.Wpf.Shell.Main;
using Microsoft.Extensions.DependencyInjection;

namespace FileMerger.Wpf;

public partial class App : System.Windows.Application
{
    private CrashDiagnosticsService? _crashDiagnostics;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _crashDiagnostics = CrashDiagnosticsService.CreateDefault();
        _crashDiagnostics.Register(this);

        try
        {
            base.OnStartup(e);

            Services = ServiceConfigurator.Configure();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IApplicationPreferencesStore preferencesStore =
                Services.GetRequiredService<IApplicationPreferencesStore>();

            await preferencesStore.InitializeAsync();

            MainViewModel mainViewModel = Services.GetRequiredService<MainViewModel>();
            await mainViewModel.InitializeAsync();

            MainWindow window = new()
            {
                DataContext = mainViewModel
            };

            window.Show();
        }
        catch (Exception ex)
        {
            _crashDiagnostics.WriteCrashLog(ex, "Startup");
            throw;
        }
    }
}