using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;

namespace FileMerger.Application.Abstractions.Services;

public interface IFileInclusionOverrideService
{
    IReadOnlyCollection<InputFile> ApplyOverrides(
        IReadOnlyCollection<InputFile> files,
        IReadOnlyCollection<FileInclusionOverride> overrides);
}