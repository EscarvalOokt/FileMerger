using FileMerger.Domain.Enums;

namespace FileMerger.Domain.ValueObjects
{
    public sealed record ValidationIssue
    {
        public ValidationIssue(
            ValidationSeverity severity,
            string code,
            string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be empty.", nameof(code));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty.", nameof(message));

            Severity = severity;
            Code = code;
            Message = message;
        }

        public ValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
    }
}