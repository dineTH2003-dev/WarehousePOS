using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WarehousePOS.Desktop.Converters;

/// <summary>Calculates a proportional width value given a current value and maximum value.</summary>
public sealed class ValueToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            decimal.TryParse(values[0]?.ToString(), out var currentVal) &&
            decimal.TryParse(values[1]?.ToString(), out var maxVal) &&
            maxVal > 0)
        {
            double containerWidth = 200;
            if (values.Length >= 3 && double.TryParse(values[2]?.ToString(), out var width) && width > 0)
            {
                containerWidth = width;
            }

            var ratio = (double)(currentVal / maxVal);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            return Math.Max(4, ratio * containerWidth);
        }

        return 4.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Calculates a relative height for column charts based on value vs max value.</summary>
public sealed class ValueToHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            decimal.TryParse(values[0]?.ToString(), out var currentVal) &&
            decimal.TryParse(values[1]?.ToString(), out var maxVal) &&
            maxVal > 0)
        {
            double maxBarHeight = 120;
            if (values.Length >= 3 && double.TryParse(values[2]?.ToString(), out var height) && height > 0)
            {
                maxBarHeight = height;
            }

            var ratio = (double)(currentVal / maxVal);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            return Math.Max(2, ratio * maxBarHeight);
        }

        return 2.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Formats a decimal ratio as a percentage string (e.g. 0.254 -> "25.4%").</summary>
public sealed class DecimalToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return $"{d:P1}";
        if (value is double db)
            return $"{db:P1}";
        return "0.0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Returns Visible if object is not null, Collapsed otherwise.</summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
