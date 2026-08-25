namespace FileMerger.Wpf.Shared.ViewModels;

public interface IAsyncCloseGuard
{
    Task<bool> CanCloseAsync();
}