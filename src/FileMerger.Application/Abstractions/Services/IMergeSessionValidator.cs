using FileMerger.Domain.Entities;
using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IMergeSessionValidator
{
    IReadOnlyCollection<ValidationIssue> Validate(MergeSession session);
}