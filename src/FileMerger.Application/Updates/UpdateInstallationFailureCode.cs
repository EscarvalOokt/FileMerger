namespace FileMerger.Application.Updates;

public enum UpdateInstallationFailureCode
{
    None = 0,
    PackageNoLongerValid,
    IncompatiblePackage,
    InvalidStaging,
    InstallationDirectoryUnavailable,
    InstallationDirectoryNotWritable,
    UpdaterRuntimeUnavailable,
    PreparationFailed,
    HelperLaunchFailed
}