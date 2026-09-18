using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.Abstractions.Services;

public interface IUnsupportedTextFileDetector
{
    bool IsTextCandidate(string filePath, UnsupportedTextFallbackOptions options);
}