using FileMerger.Wpf.Features.Profile.Models;

namespace FileMerger.Wpf.Features.Profile.ViewModels;

public sealed class ProfileTemplateItemViewModel
{
    public ProfileTemplateItemViewModel(ProfileLibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Entry = entry;
    }

    public ProfileLibraryEntry Entry { get; }

    public string Id => Entry.Id;
    public string DisplayName => Entry.DisplayName;
    public string Description => Entry.Description;

    public string SourceLabel => $"Template: {Entry.DisplayName}";
}