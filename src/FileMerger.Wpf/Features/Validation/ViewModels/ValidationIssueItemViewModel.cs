using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Validation.ViewModels;

public sealed class ValidationIssueItemViewModel : ViewModelBase
{
    public ValidationIssueItemViewModel(ValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        Model = issue;
    }

    public ValidationIssue Model { get; }

    public ValidationSeverity SeverityValue => Model.Severity;
    public string Severity => Model.Severity.ToString();
    public string Code => Model.Code;
    public string Message => Model.Message;

    public StatusSeverity SeverityStatus => Model.Severity switch
    {
        ValidationSeverity.Info => StatusSeverity.Info,
        ValidationSeverity.Warning => StatusSeverity.Warning,
        ValidationSeverity.Error => StatusSeverity.Error,
        _ => StatusSeverity.None
    };
}