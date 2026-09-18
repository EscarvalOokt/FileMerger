namespace FileMerger.Application.Abstractions.Services;

public interface IApplicationVersionProvider
{
    string GetCurrentVersion();
}