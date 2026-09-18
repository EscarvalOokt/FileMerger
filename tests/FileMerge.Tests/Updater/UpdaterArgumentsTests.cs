using System.IO;
using FileMerger.UpdateProtocol;
using FileMerger.Updater;

namespace FileMerger.Tests.Updater;

public sealed class UpdaterArgumentsTests
{
    public static TheoryData<string[]> InvalidArguments =>
    [
        [],
        [UpdateProtocolConstants.RequestArgument],
        ["--unknown", "value"],
        [UpdateProtocolConstants.RequestArgument, "relative.json"],
        [UpdateProtocolConstants.RequestArgument, Path.GetFullPath("request.json"), "extra"]
    ];

    [Fact]
    public void Parse_Should_Accept_Request_Argument_With_Absolute_Path()
    {
        string path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "request.json"));

        var result = UpdaterArguments.Parse([UpdateProtocolConstants.RequestArgument, path]);

        Assert.Equal(path, result.RequestPath);
    }

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void Parse_Should_Reject_Invalid_Arguments(string[] arguments)
    {
        Assert.Throws<ArgumentException>(() => UpdaterArguments.Parse(arguments));
    }
}