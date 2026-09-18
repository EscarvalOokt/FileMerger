using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IFileFilterService
{
    IReadOnlyCollection<InputFile> ApplyFilters(IReadOnlyCollection<InputFile> files, MergeProfile profile);
}