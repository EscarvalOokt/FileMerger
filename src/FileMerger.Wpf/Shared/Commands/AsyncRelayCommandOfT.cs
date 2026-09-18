using System.Windows.Input;

namespace FileMerger.Wpf.Shared.Commands;

public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Predicate<T?>? _canExecute;
    private readonly Func<T?, Task> _execute;

    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
            return false;

        if (!TryGetParameter(parameter, out T? typedParameter))
            return false;

        return _canExecute?.Invoke(typedParameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        TryGetParameter(parameter, out T? typedParameter);

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(typedParameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool TryGetParameter(object? parameter, out T? typedParameter)
    {
        if (parameter is null)
        {
            typedParameter = default;
            return default(T) is null;
        }

        if (parameter is T typed)
        {
            typedParameter = typed;
            return true;
        }

        typedParameter = default;
        return false;
    }
}