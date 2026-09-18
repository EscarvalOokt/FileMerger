using FileMerger.UpdateProtocol;

namespace FileMerger.Updater;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        UpdaterArguments? arguments = null;
        UpdateProtocolFileStore protocolFileStore = new();

        try
        {
            arguments = UpdaterArguments.Parse(args);
            UpdateInstallerEngine engine = new(
                protocolFileStore,
                new SystemUpdaterProcessService(),
                new UpdateInstallationFileTransaction());

            bool installed = await engine.RunAsync(arguments.RequestPath);
            return installed ? 0 : 1;
        }
        catch (Exception ex)
        {
            if (arguments is not null)
                await TryRecordTopLevelFailureAsync(protocolFileStore, arguments.RequestPath, ex.Message);

            return 1;
        }
    }

    private static async Task TryRecordTopLevelFailureAsync(
        UpdateProtocolFileStore protocolFileStore,
        string requestPath,
        string message)
    {
        try
        {
            UpdateInstallationRequest request = await protocolFileStore.ReadRequestAsync(requestPath);
            string requestDirectory = Path.GetDirectoryName(Path.GetFullPath(requestPath)) ?? string.Empty;

            if (requestDirectory.Length == 0 ||
                !UpdaterPathUtility.IsPathInsideDirectory(request.ReceiptPath, requestDirectory))
            {
                return;
            }

            UpdateInstallationReceipt receipt = File.Exists(request.ReceiptPath)
                ? await protocolFileStore.ReadReceiptAsync(request.ReceiptPath)
                : new UpdateInstallationReceipt
                {
                    AttemptId = request.AttemptId,
                    PackageId = request.PackageId,
                    ExpectedApplicationVersion = request.ExpectedApplicationVersion,
                    InstallationDirectory = request.InstallationDirectory,
                    RollbackDirectory = request.RollbackDirectory,
                    AcknowledgementPath = request.AcknowledgementPath,
                    VerificationToken = request.VerificationToken
                };

            await protocolFileStore.WriteReceiptAsync(
                request.ReceiptPath,
                receipt with
                {
                    Status = UpdateInstallationStatus.Failed,
                    Message = $"The update installer terminated unexpectedly: {message}",
                    UpdatedAtUtc = DateTime.UtcNow
                });
        }
        catch
        {
            // The original updater failure takes precedence over best-effort receipt persistence.
        }
    }
}