using System.IO;
using FileMerger.Domain.Entities;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Tests.Wpf.TestSupport;
using FileMerger.Wpf.Features.Files.ViewModels;
using FileMerger.Wpf.Features.Workspace;
using FileMerger.Wpf.Features.Workspace.Configuration;

namespace FileMerger.Tests.Wpf.Features.Workspace.Configuration;

public sealed class WorkspaceConfigurationMapperTests
{
    [Fact]
    public void Capture_Should_Read_Workspace_Configuration()
    {
        WorkspaceDocumentViewModel document = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Configured Session",
            outputPath: @"D:\Output\configured.txt");

        document.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Project",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true,
                exclusions:
                [
                    new MergeSourceExclusion(
                        "bin",
                        MergeSourceExclusionType.Directory,
                        isEnabled: true)
                ]),
            new MergeSource(
                @"D:\Project\README.md",
                MergeSourceType.File,
                isRecursive: false,
                isEnabled: false)
        ]);

        document.ProfileEditor.WorkingProfileName = "Workspace Profile";
        document.ProfileEditor.IncludeHeaderComment = true;
        document.CurrentProfileEntryId = "profile-entry";
        document.ProfileOriginEntryId = "profile-entry";
        document.ProfileOriginDisplayName = "Workspace Profile";

        WorkspaceConfigurationSnapshot result = WorkspaceConfigurationMapper.Capture(document);

        Assert.Equal("Configured Session", result.SessionName);
        Assert.Equal(@"D:\Output\configured.txt", result.OutputPath);
        Assert.Equal("Workspace Profile", result.ProfileDisplayName);
        Assert.Equal("profile-entry", result.ProfileEntryId);
        Assert.Equal("profile-entry", result.ProfileOriginEntryId);
        Assert.Equal("Workspace Profile", result.ProfileOriginDisplayName);
        Assert.True(result.Profile.IncludeHeaderComment);

        Assert.Collection(
            result.Sources,
            source =>
            {
                Assert.Equal(@"D:\Project", source.Path);
                Assert.Equal(MergeSourceType.Directory, source.Type);
                Assert.True(source.IsRecursive);
                Assert.True(source.IsEnabled);
                MergeSourceExclusion exclusion = Assert.Single(source.Exclusions);
                Assert.Equal("bin", exclusion.RelativePath);
                Assert.Equal(MergeSourceExclusionType.Directory, exclusion.Type);
                Assert.True(exclusion.IsEnabled);
            },
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            source =>
            {
                Assert.Equal(@"D:\Project\README.md", source.Path);
                Assert.Equal(MergeSourceType.File, source.Type);
                Assert.False(source.IsRecursive);
                Assert.False(source.IsEnabled);
                Assert.Empty(source.Exclusions);
            });
    }

    [Fact]
    public void Apply_Should_Update_Only_Workspace_Configuration()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Source Session",
            outputPath: @"D:\Output\source.txt");
        source.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Source",
                MergeSourceType.Directory,
                isRecursive: false,
                isEnabled: true)
        ]);
        source.ProfileEditor.WorkingProfileName = "Source Profile";
        source.ProfileEditor.IncludeHeaderComment = true;
        source.CurrentProfileEntryId = "source-profile";
        source.ProfileOriginEntryId = "origin-profile";
        source.ProfileOriginDisplayName = "Origin Profile";

        WorkspaceConfigurationSnapshot configuration = WorkspaceConfigurationMapper.Capture(source);

        WorkspaceDocumentViewModel target = WorkspaceDocumentTestFactory.CreateDocument(
            sessionName: "Target Session",
            outputPath: @"D:\Output\target.txt");
        target.WorkspaceFilePath = @"D:\Workspaces\target.filemerger.workspace.json";
        target.PreviewContent = "existing preview";
        target.SetLastOutput(CreateOutput(@"D:\Output\target.txt"));

        InputFileItemViewModel file = CreateFile("File.cs");
        target.FilesPane.LoadFiles([file]);
        target.FilesPane.ApplyOverridesDictionary(
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [file.FullPath] = false
            });

        WorkspaceConfigurationMapper.Apply(target, configuration);

        Assert.Equal("Source Session", target.SessionSettings.SessionName);
        Assert.Equal(@"D:\Output\source.txt", target.SessionSettings.OutputPath);
        Assert.Equal("Source Profile", target.CurrentProfileName);
        Assert.Equal("source-profile", target.CurrentProfileEntryId);
        Assert.Equal("origin-profile", target.ProfileOriginEntryId);
        Assert.Equal("Origin Profile", target.ProfileOriginDisplayName);
        Assert.True(target.ProfileEditor.IncludeHeaderComment);

        MergeSource appliedSource = Assert.Single(target.SourcesPane.BuildSources());
        Assert.Equal(@"D:\Source", appliedSource.Path);
        Assert.False(appliedSource.IsRecursive);

        Assert.Equal(@"D:\Workspaces\target.filemerger.workspace.json", target.WorkspaceFilePath);
        Assert.Equal("existing preview", target.PreviewContent);
        Assert.NotNull(target.LastOutput);
        Assert.Same(file, Assert.Single(target.FilesPane.Files));
        Assert.False(target.FilesPane.Files[0].IsIncluded);
        Assert.False(target.FilesPane.CaptureOverridesDictionary()[file.FullPath]);
    }

    [Fact]
    public void Capture_And_Apply_Should_Not_Share_Source_ViewModels()
    {
        WorkspaceDocumentViewModel source = WorkspaceDocumentTestFactory.CreateDocument();
        source.SourcesPane.LoadSources(
        [
            new MergeSource(
                @"D:\Project",
                MergeSourceType.Directory,
                isRecursive: true,
                isEnabled: true)
        ]);

        WorkspaceDocumentViewModel target = WorkspaceDocumentTestFactory.CreateDocument();

        WorkspaceConfigurationMapper.Apply(
            target,
            WorkspaceConfigurationMapper.Capture(source));

        Assert.NotSame(source.SourcesPane.Sources[0], target.SourcesPane.Sources[0]);

        target.SourcesPane.Sources[0].IsEnabled = false;

        Assert.True(source.SourcesPane.Sources[0].IsEnabled);
        Assert.False(target.SourcesPane.Sources[0].IsEnabled);
    }

    private static InputFileItemViewModel CreateFile(string relativePath)
    {
        var model = new InputFile(
            fullPath: $@"D:\Project\{relativePath}",
            relativePath: relativePath,
            extension: Path.GetExtension(relativePath),
            kind: FileKind.CSharp,
            isIncluded: true);

        return new InputFileItemViewModel(
            model,
            automaticIncluded: true,
            currentIncluded: true,
            appliedIncluded: true);
    }

    private static MergeOutput CreateOutput(string outputPath)
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
            outputTarget: new OutputTarget(outputPath));
    }
}