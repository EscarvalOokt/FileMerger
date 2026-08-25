using System.IO;
using FileMerger.Wpf.Diagnostics;

namespace FileMerger.Tests.Wpf.Diagnostics;

public sealed class CrashLogWriterTests : IDisposable
{
    private readonly string _tempRoot;

    public CrashLogWriterTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(CrashLogWriterTests),
            Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Write_Should_Create_Directory_And_Write_Formatted_Log()
    {
        CrashLogPathPolicy pathPolicy = new(_tempRoot);
        CrashLogFormatter formatter = new();
        CrashLogWriter writer = new(pathPolicy, formatter);

        Exception exception = CreateExceptionWithStackTrace();
        CrashLogContext context = CreateContext("WriterTest");

        CrashLogWriteResult result = writer.Write(exception, context);

        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Path);
        Assert.Null(result.Error);

        Assert.True(Directory.Exists(pathPolicy.GetCrashLogDirectory()));
        Assert.True(File.Exists(result.Path));

        string content = File.ReadAllText(result.Path);

        Assert.Contains("FileMerger crash log", content);
        Assert.Contains("Source: WriterTest", content);
        Assert.Contains("Writer failure", content);
    }

    [Fact]
    public void Write_Should_Return_Failure_When_Log_Cannot_Be_Written()
    {
        CrashLogPathPolicy pathPolicy = new("\0");
        CrashLogFormatter formatter = new();
        CrashLogWriter writer = new(pathPolicy, formatter);

        Exception exception = new InvalidOperationException("Writer failure");
        CrashLogContext context = CreateContext("WriterFailureTest");

        CrashLogWriteResult result = writer.Write(exception, context);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.Path);
        Assert.NotNull(result.Error);
    }

    private static CrashLogContext CreateContext(string source)
    {
        return new CrashLogContext(
            OccurredAtUtc: new DateTime(2026, 6, 23, 18, 42, 11, 123, DateTimeKind.Utc),
            ApplicationName: "FileMerger.Tests",
            ApplicationVersion: "1.2.3",
            InformationalVersion: "1.2.3+test",
            RuntimeVersion: ".NET Test Runtime",
            OSVersion: "Test OS",
            ProcessArchitecture: "X64",
            Is64BitProcess: true,
            BaseDirectory: @"C:\FileMerger",
            CurrentDirectory: @"C:\Workspace",
            ExceptionSource: source);
    }

    private static Exception CreateExceptionWithStackTrace()
    {
        try
        {
            throw new InvalidOperationException("Writer failure");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}