using FileMerger.Wpf.Diagnostics;

namespace FileMerger.Tests.Wpf.Diagnostics;

public sealed class CrashLogFormatterTests
{
    [Fact]
    public void Format_Should_Include_Context_And_Exception_Details()
    {
        Exception exception = CreateExceptionWithStackTrace();
        CrashLogContext context = CreateContext("UnitTest");

        string result = CrashLogFormatter.Format(exception, context);

        Assert.Contains("FileMerger crash log", result);
        Assert.Contains("Timestamp UTC: 2026-06-23T18:42:11.1230000Z", result);
        Assert.Contains("Source: UnitTest", result);

        Assert.Contains("Application:", result);
        Assert.Contains("Name: FileMerger.Tests", result);
        Assert.Contains("Version: 1.2.3", result);
        Assert.Contains("InformationalVersion: 1.2.3+test", result);

        Assert.Contains("Runtime:", result);
        Assert.Contains(".NET: .NET Test Runtime", result);
        Assert.Contains("OS: Test OS", result);
        Assert.Contains("Architecture: X64", result);
        Assert.Contains("Is64BitProcess: True", result);

        Assert.Contains("Process:", result);
        Assert.Contains(@"BaseDirectory: C:\FileMerger", result);
        Assert.Contains(@"CurrentDirectory: C:\Workspace", result);

        Assert.Contains("Exception:", result);
        Assert.Contains("System.InvalidOperationException", result);
        Assert.Contains("Top failure", result);
        Assert.Contains("Stack trace:", result);

        Assert.Contains("Inner exception:", result);
        Assert.Contains("System.ArgumentException", result);
        Assert.Contains("Inner failure", result);
    }

    [Fact]
    public void Format_Should_Include_Aggregate_Exception_Inner_Exceptions()
    {
        AggregateException exception = new(
            "Aggregate failure",
            new InvalidOperationException("First failure"),
            new ArgumentException("Second failure"));

        CrashLogContext context = CreateContext("AggregateTest");

        string result = CrashLogFormatter.Format(exception, context);

        Assert.Contains("System.AggregateException", result);
        Assert.Contains("Aggregate failure", result);
        Assert.Contains("Aggregate inner exception #1:", result);
        Assert.Contains("First failure", result);
        Assert.Contains("Aggregate inner exception #2:", result);
        Assert.Contains("Second failure", result);
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
            throw new InvalidOperationException("Top failure", new ArgumentException("Inner failure"));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}