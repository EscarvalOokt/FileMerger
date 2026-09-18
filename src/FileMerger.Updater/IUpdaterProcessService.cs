namespace FileMerger.Updater;

public interface IUpdaterProcessService
{
    Task<bool> WaitForProcessExitAsync(int processId, TimeSpan timeout, CancellationToken cancellationToken = default);

    int StartProcess(string executablePath, IReadOnlyList<string> arguments);

    Task TryTerminateProcessAsync(int processId, CancellationToken cancellationToken = default);
}