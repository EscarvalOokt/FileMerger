using FileMerger.Domain.ValueObjects;

namespace FileMerger.Application.UseCases.Common;

public sealed record ContentReadResult
{
    private ContentReadResult(
        bool isSuccessful,
        string? content,
        string? encodingName,
        ValidationIssue? issue)
    {
        IsSuccessful = isSuccessful;
        Content = content;
        EncodingName = encodingName;
        Issue = issue;
    }

    public bool IsSuccessful { get; }
    public string? Content { get; }
    public string? EncodingName { get; }
    public ValidationIssue? Issue { get; }

    public static ContentReadResult Success(string content, string? encodingName)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new ContentReadResult(true, content, encodingName, null);
    }

    public static ContentReadResult Failure(ValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return new ContentReadResult(false, null, null, issue);
    }
}