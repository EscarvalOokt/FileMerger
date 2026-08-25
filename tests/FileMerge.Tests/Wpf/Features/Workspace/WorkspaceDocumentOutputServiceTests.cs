using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.SaveOutput;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.Fakes;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Features.Workspace;

public sealed class WorkspaceDocumentOutputServiceTests
{
    [Fact]
    public async Task SaveOutputAsync_Should_Do_Nothing_When_LastOutput_Is_Null()
    {
        FakeMergeWriter writer = new();
        WorkspaceDocumentOutputService service = CreateService(writer, new FakeSaveFileDialogService());
        WorkspaceDocumentViewModel document = CreateDocument();

        await service.SaveOutputAsync(document);

        Assert.Null(writer.Output);
        Assert.Null(writer.Target);
        Assert.Equal("Ready.", document.OperationStatus.StatusMessage);
    }

    [Fact]
    public async Task SaveOutputAsync_Should_Save_LastOutput_To_Document_Target()
    {
        FakeMergeWriter writer = new();
        WorkspaceDocumentOutputService service = CreateService(writer, new FakeSaveFileDialogService());
        WorkspaceDocumentViewModel document = CreateDocument();

        MergeOutput output = CreateOutput();
        document.SetLastOutput(output);
        document.SessionSettings.OutputPath = @"D:\Output\saved.txt";

        await service.SaveOutputAsync(document);

        Assert.Same(output, writer.Output);
        Assert.NotNull(writer.Target);
        Assert.Equal(@"D:\Output\saved.txt", writer.Target!.Path);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Merged output saved successfully.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public async Task SaveOutputAsync_Should_Report_Error_Status_When_Writer_Fails()
    {
        FakeMergeWriter writer = new()
        {
            ExceptionToThrow = new InvalidOperationException("Write failed.")
        };

        WorkspaceDocumentOutputService service = CreateService(writer, new FakeSaveFileDialogService());
        WorkspaceDocumentViewModel document = CreateDocument();

        document.SetLastOutput(CreateOutput());
        document.SessionSettings.OutputPath = @"D:\Output\saved.txt";

        await service.SaveOutputAsync(document);

        Assert.False(document.OperationStatus.IsBusy);
        Assert.Equal("Failed to save merged output: Write failed.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public void BrowseOutputPath_Should_Set_Document_OutputPath_When_Path_Selected()
    {
        FakeSaveFileDialogService dialog = new()
        {
            SelectedPath = @"D:\Output\selected.txt"
        };

        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), dialog);
        WorkspaceDocumentViewModel document = CreateDocument();

        service.BrowseOutputPath(document);

        Assert.Equal(@"D:\Output\selected.txt", document.SessionSettings.OutputPath);
    }

    [Fact]
    public void BrowseOutputPath_Should_Not_Change_OutputPath_When_Canceled()
    {
        FakeSaveFileDialogService dialog = new()
        {
            SelectedPath = null
        };

        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), dialog);
        WorkspaceDocumentViewModel document = CreateDocument();

        document.SessionSettings.OutputPath = @"D:\Output\existing.txt";

        service.BrowseOutputPath(document);

        Assert.Equal(@"D:\Output\existing.txt", document.SessionSettings.OutputPath);
    }

    [Fact]
    public async Task SaveOutputAsync_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), new FakeSaveFileDialogService());

        ArgumentNullException ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SaveOutputAsync(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void BrowseOutputPath_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), new FakeSaveFileDialogService());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.BrowseOutputPath(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void CanOpenOutputFolder_Should_Return_False_When_OutputPath_Is_Empty()
    {
        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService());

        WorkspaceDocumentViewModel document = CreateDocument();

        document.SessionSettings.OutputPath = string.Empty;

        Assert.False(service.CanOpenOutputFolder(document));
    }

    [Fact]
    public void CanOpenOutputFolder_Should_Return_False_When_OutputDirectory_Is_Unavailable()
    {
        FakeFileSystemLauncher launcher = new();

        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService(),
            launcher);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        Assert.False(service.CanOpenOutputFolder(document));
    }

    [Fact]
    public void CanOpenOutputFolder_Should_Return_True_When_OutputDirectory_Is_Available()
    {
        FakeFileSystemLauncher launcher = new();
        launcher.ExistingDirectories.Add(@"D:\Output");

        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService(),
            launcher);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        Assert.True(service.CanOpenOutputFolder(document));
    }

    [Fact]
    public void OpenOutputFolder_Should_Open_OutputDirectory()
    {
        FakeFileSystemLauncher launcher = new();
        launcher.ExistingDirectories.Add(@"D:\Output");

        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService(),
            launcher);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        service.OpenOutputFolder(document);

        Assert.Equal(@"D:\Output", launcher.LastOpenedDirectoryPath);
        Assert.Equal(1, launcher.OpenDirectoryCalls);
        Assert.Equal("Output folder opened.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Success, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public void OpenOutputFolder_Should_Set_Warning_Status_When_OutputFolder_Is_Unavailable()
    {
        FakeFileSystemLauncher launcher = new();

        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService(),
            launcher);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        service.OpenOutputFolder(document);

        Assert.Null(launcher.LastOpenedDirectoryPath);
        Assert.Equal(0, launcher.OpenDirectoryCalls);
        Assert.Equal("Output folder is not available.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Warning, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public void OpenOutputFolder_Should_Set_Error_Status_When_Launcher_Fails()
    {
        FakeFileSystemLauncher launcher = new()
        {
            ExceptionToThrow = new InvalidOperationException("Explorer failed.")
        };

        launcher.ExistingDirectories.Add(@"D:\Output");

        WorkspaceDocumentOutputService service = CreateService(
            new FakeMergeWriter(),
            new FakeSaveFileDialogService(),
            launcher);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = @"D:\Output\merged.txt";

        service.OpenOutputFolder(document);

        Assert.Equal("Failed to open output folder: Explorer failed.", document.OperationStatus.StatusMessage);
        Assert.Equal(StatusSeverity.Error, document.OperationStatus.StatusSeverity);
    }

    [Fact]
    public void CanOpenOutputFolder_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), new FakeSaveFileDialogService());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.CanOpenOutputFolder(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void OpenOutputFolder_Should_Throw_When_Document_Is_Null()
    {
        WorkspaceDocumentOutputService service = CreateService(new FakeMergeWriter(), new FakeSaveFileDialogService());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            service.OpenOutputFolder(null!));

        Assert.Equal("document", ex.ParamName);
    }

    private static WorkspaceDocumentOutputService CreateService(
        FakeMergeWriter writer,
        FakeSaveFileDialogService dialog,
        FakeFileSystemLauncher? fileSystemLauncher = null)
    {
        return new WorkspaceDocumentOutputService(
            new SaveMergeOutputUseCase(writer),
            dialog,
            new MainStateFactory(),
            fileSystemLauncher ?? new FakeFileSystemLauncher());
    }

    private static WorkspaceDocumentViewModel CreateDocument()
    {
        return WorkspaceDocumentTestFactory.CreateDocument();
    }

    private static MergeOutput CreateOutput()
    {
        return new MergeOutput(
            content: "merged",
            sections: [],
            statistics: new MergeStatistics(
                filesScanned: 1,
                filesIncluded: 1,
                filesSkipped: 0,
                totalCharacters: 6,
                duration: TimeSpan.Zero),
            generatedAtUtc: DateTime.UtcNow,
            outputTarget: new OutputTarget(@"D:\Output\merged.txt"));
    }

    private sealed class FakeMergeWriter : IMergeWriter
    {
        public MergeOutput? Output { get; private set; }
        public OutputTarget? Target { get; private set; }
        public Exception? ExceptionToThrow { get; init; }

        public void Write(MergeOutput output, OutputTarget target)
        {
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            Output = output;
            Target = target;
        }
    }
}