namespace FileMerger.Application.Abstractions.Services;

public interface IUpdaterVersionProvider
{
    string GetCurrentUpdaterVersion();
}