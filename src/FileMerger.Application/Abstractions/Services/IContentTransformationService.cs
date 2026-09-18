using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IContentTransformationService
{
    string Transform(string content, InputFile file, MergeProfile profile);
}