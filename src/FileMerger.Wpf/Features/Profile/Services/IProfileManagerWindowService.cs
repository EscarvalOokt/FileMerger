namespace FileMerger.Wpf.Features.Profile.Services;

public interface IProfileManagerWindowService
{
    Task ShowDialogAsync();

    Task ShowDialogAsync(ICurrentSessionProfileHost profileHost, ProfileManagerContext context);
}