namespace FileMerger.Application.Updates;

public sealed class UpdateCompatibilityPolicy
{
    private const string SupportedArchitecture = "any";
    private const string SupportedDeployment = "frameworkDependent";
    private const string SupportedFormat = "zip";
    private const string SupportedFramework = "net10.0-windows";
    private const string SupportedOs = "windows";

    public bool IsReleaseEligible(SemanticVersion currentVersion, SemanticVersion releaseVersion)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(releaseVersion);

        if (releaseVersion.CompareTo(currentVersion) <= 0)
            return false;

        if (!currentVersion.IsPrerelease && releaseVersion.IsPrerelease)
            return false;

        return true;
    }

    public bool IsPackageCompatible(
        UpdatePackage package,
        SemanticVersion currentVersion,
        SemanticVersion? currentUpdaterVersion)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(currentVersion);

        if (!string.Equals(package.Os, SupportedOs, StringComparison.Ordinal) ||
            !string.Equals(package.Architecture, SupportedArchitecture, StringComparison.Ordinal) ||
            !string.Equals(package.Framework, SupportedFramework, StringComparison.Ordinal) ||
            !string.Equals(package.Deployment, SupportedDeployment, StringComparison.Ordinal) ||
            !string.Equals(package.Format, SupportedFormat, StringComparison.Ordinal))
        {
            return false;
        }

        if (package.MinimumSourceVersion is not null && currentVersion < package.MinimumSourceVersion)
            return false;

        if (package.MinimumUpdaterVersion is not null &&
            (currentUpdaterVersion is null || currentUpdaterVersion < package.MinimumUpdaterVersion))
        {
            return false;
        }

        return true;
    }
}