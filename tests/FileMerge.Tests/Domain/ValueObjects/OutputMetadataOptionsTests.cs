using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Domain.ValueObjects;

public sealed class OutputMetadataOptionsTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        OutputMetadataOptions result = new(
            IncludeBuildTimestamp: false,
            IncludeSessionName: false,
            IncludeOutputPath: false,
            IncludeFileSummary: false,
            SkippedFilesMetadataMode: SkippedFilesMetadataMode.Detailed,
            IncludeSourceExcludedFiles: true);

        Assert.False(result.IncludeBuildTimestamp);
        Assert.False(result.IncludeSessionName);
        Assert.False(result.IncludeOutputPath);
        Assert.False(result.IncludeFileSummary);
        Assert.Equal(SkippedFilesMetadataMode.Detailed, result.SkippedFilesMetadataMode);
        Assert.True(result.IncludeSourceExcludedFiles);
    }

    [Fact]
    public void Default_Should_Enable_Base_Header_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.True(result.IncludeBuildTimestamp);
        Assert.True(result.IncludeSessionName);
        Assert.True(result.IncludeOutputPath);
        Assert.True(result.IncludeFileSummary);
    }

    [Fact]
    public void Default_Should_Disable_Skipped_Files_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.Equal(SkippedFilesMetadataMode.None, result.SkippedFilesMetadataMode);
    }

    [Fact]
    public void Default_Should_Disable_Source_Excluded_Files_Metadata()
    {
        OutputMetadataOptions result = OutputMetadataOptions.Default;

        Assert.False(result.IncludeSourceExcludedFiles);
    }
}