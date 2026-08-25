using FileMerger.Application.UseCases.Common;
using FileMerger.Domain.Entities;

namespace FileMerger.Application.Abstractions.Services;

public interface IContentReader
{
    ContentReadResult Read(InputFile file, InputReadOptions options);
}