using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeUserPromptService : IUserPromptService
{
    private readonly Queue<UnsavedChangesDecision> _unsavedChangesDecisions = [];

    public UnsavedChangesDecision UnsavedChangesDecision { get; set; } = UnsavedChangesDecision.Cancel;

    public bool ConfirmResult { get; set; } = true;

    public int ConfirmUnsavedChangesCalls { get; private set; }

    public int ConfirmCalls { get; private set; }

    public string? LastUnsavedChangesTitle { get; private set; }

    public string? LastUnsavedChangesMessage { get; private set; }

    public string? LastConfirmTitle { get; private set; }

    public string? LastConfirmMessage { get; private set; }

    public string? LastConfirmButtonText { get; private set; }

    public string? LastCancelButtonText { get; private set; }

    public List<string> UnsavedChangesMessages { get; } = [];

    public UnsavedChangesDecision ConfirmUnsavedChanges(string title, string message)
    {
        ConfirmUnsavedChangesCalls++;

        LastUnsavedChangesTitle = title;
        LastUnsavedChangesMessage = message;

        UnsavedChangesMessages.Add(message);

        if (_unsavedChangesDecisions.Count > 0)
            return _unsavedChangesDecisions.Dequeue();

        return UnsavedChangesDecision;
    }

    public bool Confirm(string title, string message, string confirmButtonText = "Yes", string cancelButtonText = "No")
    {
        ConfirmCalls++;

        LastConfirmTitle = title;
        LastConfirmMessage = message;
        LastConfirmButtonText = confirmButtonText;
        LastCancelButtonText = cancelButtonText;

        return ConfirmResult;
    }

    public void EnqueueUnsavedChangesDecision(UnsavedChangesDecision decision)
    {
        _unsavedChangesDecisions.Enqueue(decision);
    }
}