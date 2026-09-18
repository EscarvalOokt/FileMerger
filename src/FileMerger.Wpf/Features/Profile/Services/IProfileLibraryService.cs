using FileMerger.Wpf.Features.Profile.Models;

namespace FileMerger.Wpf.Features.Profile.Services;

public interface IProfileLibraryService
{
    string GetPrimaryProfilesDirectory();

    Task<IReadOnlyCollection<ProfileLibraryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProfileLibraryEntry> LoadAsync(string id, CancellationToken cancellationToken = default);

    Task<ProfileLibraryEntry> SaveAsync(
        ProfileLibraryEntry entry,
        bool saveAsNew = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProfileLibraryEntry>> ImportAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken = default);

    Task ExportAsync(ProfileLibraryEntry entry, string targetFilePath, CancellationToken cancellationToken = default);
}