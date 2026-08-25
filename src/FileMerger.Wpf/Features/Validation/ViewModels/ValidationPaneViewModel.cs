using System.Collections.ObjectModel;
using FileMerger.Domain.Enums;
using FileMerger.Domain.ValueObjects;
using FileMerger.Wpf.Shared.Status;
using FileMerger.Wpf.Shared.ViewModels;

namespace FileMerger.Wpf.Features.Validation.ViewModels;

public sealed class ValidationPaneViewModel : ViewModelBase
{
    private int _infoCount;
    private int _warningCount;
    private int _errorCount;
    private bool _isDetailsExpanded;
    private StatusSeverity _highestStatusSeverity = StatusSeverity.None;
    private string _summaryText = "No validation issues.";
    private string _counterSummaryText = "Errors 0 · Warnings 0 · Info 0";

    public ObservableCollection<ValidationIssueItemViewModel> Issues { get; } = [];

    public int InfoCount
    {
        get => _infoCount;
        private set => SetProperty(ref _infoCount, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        private set => SetProperty(ref _warningCount, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        private set => SetProperty(ref _errorCount, value);
    }

    public int TotalCount => InfoCount + WarningCount + ErrorCount;

    public bool HasIssues => TotalCount > 0;
    public bool HasErrors => ErrorCount > 0;
    public bool HasWarnings => WarningCount > 0;
    public bool HasInfo => InfoCount > 0;
    public bool HasVisibleDetails => HasIssues && IsDetailsExpanded;

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set
        {
            if (!SetProperty(ref _isDetailsExpanded, value))
                return;

            OnPropertyChanged(nameof(HasVisibleDetails));
            OnPropertyChanged(nameof(DetailsToggleText));
        }
    }

    public string DetailsToggleText => IsDetailsExpanded
        ? "Hide details"
        : "Show details";

    public StatusSeverity HighestStatusSeverity
    {
        get => _highestStatusSeverity;
        private set => SetProperty(ref _highestStatusSeverity, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public string CounterSummaryText
    {
        get => _counterSummaryText;
        private set => SetProperty(ref _counterSummaryText, value);
    }

    public void Load(IReadOnlyCollection<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Issues.Clear();

        foreach (ValidationIssue issue in issues)
            Issues.Add(new ValidationIssueItemViewModel(issue));

        RefreshSummary();
    }

    public void Clear()
    {
        Issues.Clear();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        InfoCount = Issues.Count(x => x.SeverityValue == ValidationSeverity.Info);
        WarningCount = Issues.Count(x => x.SeverityValue == ValidationSeverity.Warning);
        ErrorCount = Issues.Count(x => x.SeverityValue == ValidationSeverity.Error);

        HighestStatusSeverity =
            ErrorCount > 0 ? StatusSeverity.Error :
            WarningCount > 0 ? StatusSeverity.Warning :
            InfoCount > 0 ? StatusSeverity.Info :
            StatusSeverity.None;

        SummaryText = BuildSummaryText();
        CounterSummaryText = BuildCounterSummaryText();

        RefreshDetailsExpansion();

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasInfo));
        OnPropertyChanged(nameof(HasVisibleDetails));
        OnPropertyChanged(nameof(DetailsToggleText));
    }

    private void RefreshDetailsExpansion()
    {
        if (TotalCount == 0)
        {
            IsDetailsExpanded = false;
            return;
        }

        if (HasErrors || HasWarnings)
            IsDetailsExpanded = true;
    }

    private string BuildSummaryText()
    {
        if (TotalCount == 0)
            return "No validation issues.";

        List<string> parts = [];

        if (ErrorCount > 0)
            parts.Add($"{ErrorCount} error(s)");

        if (WarningCount > 0)
            parts.Add($"{WarningCount} warning(s)");

        if (InfoCount > 0)
            parts.Add($"{InfoCount} info");

        return string.Join(" · ", parts);
    }

    private string BuildCounterSummaryText()
    {
        return $"Errors {ErrorCount} · Warnings {WarningCount} · Info {InfoCount}";
    }
}