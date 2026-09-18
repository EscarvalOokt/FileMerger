namespace FileMerger.UpdateProtocol;

public static class UpdateProtocolConstants
{
    public const int SchemaVersion = 1;
    public const string UpdaterProtocolVersion = "1.0.0";

    public const string UpdaterExecutableFileName = "FileMerger.Updater.exe";
    public const string UpdaterAssemblyFileName = "FileMerger.Updater.dll";
    public const string UpdaterDependenciesFileName = "FileMerger.Updater.deps.json";
    public const string UpdaterRuntimeConfigFileName = "FileMerger.Updater.runtimeconfig.json";
    public const string ProtocolAssemblyFileName = "FileMerger.UpdateProtocol.dll";

    public const string RequestArgument = "--request";
    public const string VerificationReceiptArgument = "--update-verification-receipt";
    public const string VerificationTokenArgument = "--update-verification-token";

    public static IReadOnlyList<string> RequiredUpdaterRuntimeFiles { get; } =
    [
        UpdaterExecutableFileName,
        UpdaterAssemblyFileName,
        UpdaterDependenciesFileName,
        UpdaterRuntimeConfigFileName,
        ProtocolAssemblyFileName
    ];
}