using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WarehousePOS.Desktop.Converters;

/// <summary>Converts bool to Visibility. True → Visible, False → Collapsed.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>Inverts a bool. True → False, False → True.</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && !b;
}

/// <summary>Converts null to Collapsed, non-null to Visible.</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Formats a decimal as currency string (e.g. "Rs. 1,250.00").</summary>
[ValueConversion(typeof(decimal), typeof(string))]
public sealed class DecimalToCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is decimal d ? $"Rs. {d:N2}" : "Rs. 0.00";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        decimal.TryParse(value?.ToString()?.Replace("Rs.", "").Trim(), out var result) ? result : 0m;
}

/// <summary>Converts UTC DateTime to local time string for display.</summary>
[ValueConversion(typeof(DateTime), typeof(string))]
public sealed class UtcToLocalDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime utc)
            return utc.ToLocalTime().ToString("dd MMM yyyy  hh:mm tt");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts bool IsActive to "Active" / "Inactive" status label.</summary>
[ValueConversion(typeof(bool), typeof(string))]
public sealed class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Active" : "Inactive";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts bool IsActive to toggle button text ("Deactivate" / "Activate").</summary>
[ValueConversion(typeof(bool), typeof(string))]
public sealed class BoolToToggleTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Deactivate" : "Activate";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>IsEditing bool → form title string.</summary>
[ValueConversion(typeof(bool), typeof(string))]
public sealed class IsEditingToTitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "Edit Category" : "New Category";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Compares an Enum value with a string parameter for RadioButton checked binding.</summary>
public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return value.ToString()!.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string paramString)
            return Enum.Parse(targetType, paramString);
        return Binding.DoNothing;
    }
}
