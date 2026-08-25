using System.Windows.Threading;

namespace FileMerger.Wpf.Diagnostics;

public sealed class CrashDiagnosticsService
{
    private readonly CrashLogWriter _writer;

    private bool _isRegistered;

    public CrashDiagnosticsService(CrashLogWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    public static CrashDiagnosticsService CreateDefault()
    {
        CrashLogPathPolicy pathPolicy = new();
        CrashLogFormatter formatter = new();
        CrashLogWriter writer = new(pathPolicy, formatter);

        return new CrashDiagnosticsService(writer);
    }

    public void Register(System.Windows.Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (_isRegistered)
            return;

        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _isRegistered = true;
    }

    public CrashLogWriteResult WriteCrashLog(Exception exception, string source)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var context = CrashLogContext.Create(source);

        return _writer.Write(exception, context);
    }

    private void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception, "WPF DispatcherUnhandledException");

        e.Handled = false;
    }

    private void OnAppDomainUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        Exception exception = e.ExceptionObject as Exception
                              ?? new InvalidOperationException(
                                  $"Unhandled non-exception object: {e.ExceptionObject}");

        WriteCrashLog(exception, "AppDomain.UnhandledException");
    }

    private void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception, "TaskScheduler.UnobservedTaskException");
    }
}