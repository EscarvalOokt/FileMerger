using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.SaveOutput;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Tests.Application.UseCases;

public sealed class SaveMergeOutputUseCaseTests
{
    [Fact]
    public void Execute_Should_Throw_When_Request_Is_Null()
    {
        var useCase = new SaveMergeOutputUseCase(new FakeMergeWriter());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public void Execute_Should_Call_Writer_And_Return_Success_When_Request_Is_Valid()
    {
        var writer = new FakeMergeWriter();
        var useCase = new SaveMergeOutputUseCase(writer);

        MergeOutput output = CreateOutput();
        var target = new OutputTarget(@"D:\Output\merged.txt");
        var request = new SaveMergeOutputRequest(output, target);

        SaveMergeOutputResult result = useCase.Execute(request);

        Assert.True(result.IsSuccessful);
        Assert.Equal(target, result.Target);
        Assert.Empty(result.ValidationIssues);

        Assert.Single(writer.WriteCalls);
        Assert.Equal(output, writer.WriteCalls.Single().Output);
        Assert.Equal(target, writer.WriteCalls.Single().Target);
    }

    private static MergeOutput CreateOutput()
    {
        return new MergeOutput(
            content: "merged content",
            sections: [],
            statistics: new MergeStatistics(1, 1, 0, 14, TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private sealed class FakeMergeWriter : IMergeWriter
    {
        public List<(MergeOutput Output, OutputTarget Target)> WriteCalls { get; } = [];

        public void Write(MergeOutput output, OutputTarget target)
        {
            WriteCalls.Add((output, target));
        }
    }
}