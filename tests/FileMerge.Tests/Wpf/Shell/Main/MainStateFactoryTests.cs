using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Preview.State;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Shell.Main;

namespace FileMerger.Tests.Wpf.Shell.Main;

public sealed class MainStateFactoryTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        "FileMerger.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void BuildSession_Should_Build_Session_From_Document()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        document.SessionSettings.SessionName = "Document Session";
        document.SessionSettings.OutputPath = @"D:\Output\document-output.txt";
        document.ProfileEditor.WorkingProfileName = "Document Profile";
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: @"D:\Project",
                type: MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        MainStateFactory factory = new();

        MergeSession session = factory.BuildSession(document);

        Assert.Equal("Document Session", session.Name);
        Assert.Equal(@"D:\Output\document-output.txt", session.OutputTarget.Path);
        Assert.Equal("Document Profile", session.Profile.Name);

        MergeSource source = Assert.Single(session.Sources);
        Assert.Equal(@"D:\Project", source.Path);
        Assert.Equal(MergeSourceType.Directory, source.Type);
        Assert.True(source.IsRecursive);
        Assert.True(source.IsEnabled);
    }

    [Fact]
    public void BuildPreviewState_Should_Build_State_From_Document()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        document.SessionSettings.SessionName = "Preview Session";
        document.SessionSettings.OutputPath = @"D:\Output\preview.txt";
        document.ProfileEditor.WorkingProfileName = "Preview Profile";
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: @"D:\Project",
                type: MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        InputFile file = CreateFile(@"D:\Project\Test.cs");
        document.FilesPane.ApplyFiles(
            automaticFiles: [file],
            currentFiles: [file],
            appliedInclusionState: null);

        document.FilesPane.Files.Single().IsIncluded = false;

        MainStateFactory factory = new();

        PreviewStateSnapshot snapshot = factory.BuildPreviewState(document);

        Assert.Equal("Preview Session", snapshot.Session.SessionName);
        Assert.Equal(@"D:\Output\preview.txt", snapshot.Session.OutputPath);

        Assert.Single(snapshot.Sources);
        Assert.Equal("Preview Profile", document.ProfileEditor.WorkingProfileName);

        KeyValuePair<string, bool> onlyOverride =
            Assert.Single(snapshot.FileInclusionOverrides);

        Assert.EndsWith(@"D:\Project\Test.cs", onlyOverride.Key, StringComparison.OrdinalIgnoreCase);
        Assert.False(onlyOverride.Value);
    }

    [Fact]
    public void BuildPreviewState_Should_Not_Mix_Overrides_Between_Documents()
    {
        WorkspaceDocumentViewModel firstDocument = CreateDocument();
        WorkspaceDocumentViewModel secondDocument = CreateDocument();

        InputFile firstFile = CreateFile(@"D:\First\First.cs");
        InputFile secondFile = CreateFile(@"D:\Second\Second.cs");

        firstDocument.FilesPane.ApplyFiles(
            automaticFiles: [firstFile],
            currentFiles: [firstFile],
            appliedInclusionState: null);

        secondDocument.FilesPane.ApplyFiles(
            automaticFiles: [secondFile],
            currentFiles: [secondFile],
            appliedInclusionState: null);

        firstDocument.FilesPane.Files.Single().IsIncluded = false;
        secondDocument.FilesPane.Files.Single().IsIncluded = false;

        MainStateFactory factory = new();

        PreviewStateSnapshot firstSnapshot = factory.BuildPreviewState(firstDocument);
        PreviewStateSnapshot secondSnapshot = factory.BuildPreviewState(secondDocument);

        KeyValuePair<string, bool> firstOverride =
            Assert.Single(firstSnapshot.FileInclusionOverrides);

        KeyValuePair<string, bool> secondOverride =
            Assert.Single(secondSnapshot.FileInclusionOverrides);

        Assert.EndsWith(@"D:\First\First.cs", firstOverride.Key, StringComparison.OrdinalIgnoreCase);
        Assert.False(firstOverride.Value);

        Assert.EndsWith(@"D:\Second\Second.cs", secondOverride.Key, StringComparison.OrdinalIgnoreCase);
        Assert.False(secondOverride.Value);
    }

    [Fact]
    public void BuildInitialOutputPath_Should_Use_Document_OutputPath_When_Set()
    {
        WorkspaceDocumentViewModel document = CreateDocument();

        document.SessionSettings.OutputPath = @"D:\Output\explicit.txt";
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: @"D:\Project",
                type: MergeSourceType.Directory)
        ]);

        MainStateFactory factory = new();

        string? result = factory.BuildInitialOutputPath(document);

        Assert.Equal(@"D:\Output\explicit.txt", result);
    }

    [Fact]
    public void BuildInitialOutputPath_Should_Use_Document_Directory_Source_When_OutputPath_Is_Empty()
    {
        Directory.CreateDirectory(_tempRoot);

        string sourceDirectory = Path.Combine(_tempRoot, "Source");
        Directory.CreateDirectory(sourceDirectory);

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = string.Empty;
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: sourceDirectory,
                type: MergeSourceType.Directory)
        ]);

        MainStateFactory factory = new();

        string? result = factory.BuildInitialOutputPath(document);

        Assert.Equal(
            Path.Combine(sourceDirectory, "MergedOutput.txt"),
            result);
    }

    [Fact]
    public void BuildInitialOutputPath_Should_Use_Document_File_Source_When_OutputPath_Is_Empty()
    {
        Directory.CreateDirectory(_tempRoot);

        string sourceDirectory = Path.Combine(_tempRoot, "Source");
        Directory.CreateDirectory(sourceDirectory);

        string sourceFile = Path.Combine(sourceDirectory, "Input.cs");
        File.WriteAllText(sourceFile, "content");

        WorkspaceDocumentViewModel document = CreateDocument();
        document.SessionSettings.OutputPath = string.Empty;
        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                path: sourceFile,
                type: MergeSourceType.File,
                isRecursive: false)
        ]);

        MainStateFactory factory = new();

        string? result = factory.BuildInitialOutputPath(document);

        Assert.Equal(
            Path.Combine(sourceDirectory, "MergedOutput.txt"),
            result);
    }

    [Fact]
    public void BuildSession_Should_Throw_When_Document_Is_Null()
    {
        MainStateFactory factory = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            factory.BuildSession(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void BuildPreviewState_Should_Throw_When_Document_Is_Null()
    {
        MainStateFactory factory = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            factory.BuildPreviewState(null!));

        Assert.Equal("document", ex.ParamName);
    }

    [Fact]
    public void BuildInitialOutputPath_Should_Throw_When_Document_Is_Null()
    {
        MainStateFactory factory = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            factory.BuildInitialOutputPath(null!));

        Assert.Equal("document", ex.ParamName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    private static WorkspaceDocumentViewModel CreateDocument()
    {
        return WorkspaceDocumentTestFactory.CreateDefaultDocument();
    }

    private static InputFile CreateFile(string fullPath)
    {
        return new InputFile(
            fullPath: fullPath,
            relativePath: Path.GetFileName(fullPath),
            extension: ".cs",
            kind: FileKind.CSharp);
    }
}