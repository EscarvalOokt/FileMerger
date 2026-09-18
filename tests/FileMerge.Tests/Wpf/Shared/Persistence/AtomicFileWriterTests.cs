using System.IO;
using System.Text;
using FileMerger.Wpf.Shared.Persistence;

namespace FileMerger.Tests.Wpf.Shared.Persistence;

public sealed class AtomicFileWriterTests : IDisposable
{
    private readonly string _tempRoot;

    public AtomicFileWriterTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "FileMerger.Tests",
            nameof(AtomicFileWriterTests),
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Fact]
    public async Task WriteAsync_Should_Create_New_Target()
    {
        string path = Path.Combine(_tempRoot, "document.json");

        await AtomicFileWriter.WriteAsync(path, (stream, token) => WriteTextAsync(stream, "new content", token));

        Assert.Equal("new content", await File.ReadAllTextAsync(path));
        AssertNoTemporaryFiles(path);
    }

    [Fact]
    public async Task WriteAsync_Should_Replace_Existing_Target()
    {
        string path = Path.Combine(_tempRoot, "document.json");
        await File.WriteAllTextAsync(path, "old content");

        await AtomicFileWriter.WriteAsync(
            path,
            (stream, token) => WriteTextAsync(stream, "replacement content", token));

        Assert.Equal("replacement content", await File.ReadAllTextAsync(path));
        AssertNoTemporaryFiles(path);
    }

    [Fact]
    public async Task WriteAsync_Should_Preserve_Existing_Target_When_Writer_Fails()
    {
        string path = Path.Combine(_tempRoot, "document.json");
        await File.WriteAllTextAsync(path, "old content");

        await Assert.ThrowsAsync<InvalidOperationException>(() => AtomicFileWriter.WriteAsync(
            path,
            async (stream, token) =>
            {
                await WriteTextAsync(stream, "partial replacement", token);
                throw new InvalidOperationException("Simulated write failure.");
            }));

        Assert.Equal("old content", await File.ReadAllTextAsync(path));
        AssertNoTemporaryFiles(path);
    }

    [Fact]
    public async Task WriteAsync_Should_Preserve_Existing_Target_When_Canceled_Before_Commit()
    {
        string path = Path.Combine(_tempRoot, "document.json");
        await File.WriteAllTextAsync(path, "old content");
        using CancellationTokenSource cancellation = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AtomicFileWriter.WriteAsync(
            path,
            async (stream, token) =>
            {
                await WriteTextAsync(stream, "replacement content", token);

                // ReSharper disable once AccessToDisposedClosure
                await cancellation.CancelAsync();
            },
            cancellation.Token));

        Assert.Equal("old content", await File.ReadAllTextAsync(path, CancellationToken.None));
        AssertNoTemporaryFiles(path);
    }

    private static async Task WriteTextAsync(Stream stream, string text, CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await stream.WriteAsync(bytes.AsMemory(), cancellationToken);
    }

    private static void AssertNoTemporaryFiles(string targetPath)
    {
        string directory = Path.GetDirectoryName(targetPath)!;
        string pattern = $".{Path.GetFileName(targetPath)}.*.tmp";

        Assert.Empty(Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly));
    }
}