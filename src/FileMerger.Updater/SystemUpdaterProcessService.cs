using System.Diagnostics;

namespace FileMerger.Updater;

public sealed class SystemUpdaterProcessService : IUpdaterProcessService
{
    public async Task<bool> WaitForProcessExitAsync(
        int processId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
            return true;

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return true;
        }

        using (process)
        {
            if (process.HasExited)
                return true;

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCancellation.Token);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }
    }

    public int StartProcess(string executablePath, IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        ProcessStartInfo startInfo = new(executablePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory
        };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using Process process = Process.Start(startInfo) ??
                                throw new InvalidOperationException($"Failed to start process '{executablePath}'.");

        return process.Id;
    }

    public async Task TryTerminateProcessAsync(int processId, CancellationToken cancellationToken = default)
    {
        if (processId <= 0)
            return;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
                return;

            process.Kill(entireProcessTree: true);

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await process.WaitForExitAsync(timeoutCancellation.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Termination is best-effort before rollback.
            }
        }
        catch (ArgumentException)
        {
            // The process already exited.
        }
        catch (InvalidOperationException)
        {
            // The process already exited or cannot be queried.
        }
        catch
        {
            // Termination is best-effort before rollback.
        }
    }
}