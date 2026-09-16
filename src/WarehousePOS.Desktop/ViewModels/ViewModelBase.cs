using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WarehousePOS.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels.
/// Implements INotifyPropertyChanged for WPF data binding.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Relay command implementation for binding UI actions to ViewModel methods.
/// </summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged() =>
        CommandManager.InvalidateRequerySuggested();
}

/// <summary>
/// Relay command with parameter support.
/// </summary>
public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    private static T? ConvertParameter(object? parameter)
    {
        if (parameter is T typed) return typed;
        if (parameter is null) return default;
        try
        {
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T?)Convert.ChangeType(parameter, targetType);
        }
        catch
        {
            return default;
        }
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(ConvertParameter(parameter)) ?? true;
    public void Execute(object? parameter) => execute(ConvertParameter(parameter));
}
