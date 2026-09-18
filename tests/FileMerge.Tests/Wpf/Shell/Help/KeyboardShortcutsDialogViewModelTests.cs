using FileMerger.Wpf.Shell.Help;

namespace FileMerger.Tests.Wpf.Shell.Help;

public sealed class KeyboardShortcutsDialogViewModelTests
{
    [Fact]
    public void CreateDefault_Should_Expose_Real_Shortcut_Groups()
    {
        var viewModel = KeyboardShortcutsDialogViewModel.CreateDefault();

        Assert.Collection(
            viewModel.Groups,
            group => Assert.Equal("File", group.Title),
            group => Assert.Equal("Workspace", group.Title),
            group => Assert.Equal("Profile", group.Title),
            group => Assert.Equal("Preview", group.Title),
            group => Assert.Equal("Help", group.Title));
    }

    [Theory]
    [InlineData("File", "New Workspace Tab", "Ctrl+T")]
    [InlineData("File", "Open Workspace...", "Ctrl+O")]
    [InlineData("File", "Save Workspace", "Ctrl+S")]
    [InlineData("File", "Save Workspace As...", "Ctrl+Shift+S")]
    [InlineData("Workspace", "Duplicate Tab", "Ctrl+Shift+D")]
    [InlineData("Workspace", "Rename Tab...", "F2")]
    [InlineData("Workspace", "Close Tab", "Ctrl+W")]
    [InlineData("Workspace", "Close Other Tabs", "Ctrl+Shift+W")]
    [InlineData("Workspace", "Next Tab", "Ctrl+Tab")]
    [InlineData("Workspace", "Previous Tab", "Ctrl+Shift+Tab")]
    [InlineData("Help", "Keyboard Shortcuts", "F1")]
    public void CreateDefault_Should_Include_Configured_Shortcuts(string groupTitle, string action, string gesture)
    {
        var viewModel = KeyboardShortcutsDialogViewModel.CreateDefault();

        KeyboardShortcutGroupViewModel group = Assert.Single(viewModel.Groups, x => x.Title == groupTitle);

        KeyboardShortcutItemViewModel shortcut = Assert.Single(group.Shortcuts, x => x.Action == action);

        Assert.Equal(gesture, shortcut.Gesture);
    }

    [Theory]
    [InlineData("Profile")]
    [InlineData("Preview")]
    public void CreateDefault_Should_Show_Empty_State_For_Groups_Without_Shortcuts(string groupTitle)
    {
        var viewModel = KeyboardShortcutsDialogViewModel.CreateDefault();

        KeyboardShortcutGroupViewModel group = Assert.Single(viewModel.Groups, x => x.Title == groupTitle);

        Assert.False(group.HasShortcuts);
        Assert.Equal("No keyboard shortcuts assigned yet.", group.EmptyText);
    }
}