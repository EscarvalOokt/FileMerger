using FileMerger.Wpf.Features.Profile.Models;

namespace FileMerger.Wpf.Features.Profile.Services;

public interface IBuiltInProfilePresetProvider
{
    IReadOnlyCollection<ProfileLibraryEntry> GetAll();
}