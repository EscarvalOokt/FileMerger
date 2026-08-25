using FileMerger.Wpf.Shared.Dialogs;

namespace FileMerger.Tests.Wpf.Fakes;

public sealed class FakeUserPromptService : IUserPromptService
{
    private readonly Queue<UnsavedChangesDecision> _unsavedChangesDecisions = [];

    public UnsavedChangesDecision UnsavedChangesDecision { get; set; } =
        UnsavedChangesDecision.Cancel;

    public bool ConfirmResult { get; set; } = true;

    public int ConfirmUnsavedChangesCalls { get; private set; }

    public int ConfirmCalls { get; private set; }

    public string? LastUnsavedChangesTitle { get; private set; }

    public string? LastUnsavedChangesMessage { get; private set; }

    public string? LastConfirmTitle { get; private set; }

    public string? LastConfirmMessage { get; private set; }

    public List<string> UnsavedChangesTitles { get; } = [];

    public List<string> UnsavedChangesMessages { get; } = [];

    public List<string> ConfirmTitles { get; } = [];

    public List<string> ConfirmMessages { get; } = [];

    public void EnqueueUnsavedChangesDecision(UnsavedChangesDecision decision)
    {
        _unsavedChangesDecisions.Enqueue(decision);
    }

    public UnsavedChangesDecision ConfirmUnsavedChanges(
        string title,
        string message)
    {
        ConfirmUnsavedChangesCalls++;

        LastUnsavedChangesTitle = title;
        LastUnsavedChangesMessage = message;

        UnsavedChangesTitles.Add(title);
        UnsavedChangesMessages.Add(message);

        if (_unsavedChangesDecisions.Count > 0)
            return _unsavedChangesDecisions.Dequeue();

        return UnsavedChangesDecision;
    }

    public bool Confirm(
        string title,
        string message)
    {
        ConfirmCalls++;

        LastConfirmTitle = title;
        LastConfirmMessage = message;

        ConfirmTitles.Add(title);
        ConfirmMessages.Add(message);

        return ConfirmResult;
    }
}