using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IMergeWriter
{
    void Write(MergeOutput output, OutputTarget target);
}