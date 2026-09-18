using System.Windows.Input;

namespace FileMerger.Wpf.Shared.Commands;

public sealed class RelayCommand<T> : ICommand
{
    private readonly Predicate<T?>? _canExecute;
    private readonly Action<T?> _execute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (!TryGetParameter(parameter, out T? typedParameter))
            return false;

        return _canExecute?.Invoke(typedParameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        TryGetParameter(parameter, out T? typedParameter);
        _execute(typedParameter);
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