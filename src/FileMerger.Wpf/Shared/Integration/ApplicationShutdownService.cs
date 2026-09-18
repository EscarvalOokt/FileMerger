using WpfApplication = System.Windows.Application;

namespace FileMerger.Wpf.Shared.Integration;

public sealed class ApplicationShutdownService : IApplicationShutdownService
{
    public void Shutdown()
    {
        WpfApplication application = WpfApplication.Current ??
                                     throw new InvalidOperationException("The WPF application is not running.");

        application.Shutdown();
    }
}