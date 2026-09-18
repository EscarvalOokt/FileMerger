using FileMerger.Application.Abstractions.Services;
using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Infrastructure.Services.Filtering;

public sealed class FileInclusionOverrideService : IFileInclusionOverrideService
{
    public IReadOnlyCollection<InputFile> ApplyOverrides(
        IReadOnlyCollection<InputFile> files,
        IReadOnlyCollection<FileInclusionOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(overrides);

        var overrideMap = overrides.GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);

        List<InputFile> result = [];

        foreach (InputFile file in files)
        {
            if (!overrideMap.TryGetValue(file.FullPath, out FileInclusionOverride? inclusionOverride))
            {
                result.Add(file);
                continue;
            }

            if (inclusionOverride.IsIncluded == file.IsIncluded)
            {
                result.Add(file);
                continue;
            }

            if (inclusionOverride.IsIncluded)
            {
                result.Add(file.Include());
            }
            else
            {
                result.Add(file.Exclude(new SkipReason("manual.exclude", "Excluded manually by user.")));
            }
        }

        return result;
    }
}