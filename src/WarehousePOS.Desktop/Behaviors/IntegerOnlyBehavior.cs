using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePOS.Desktop.Behaviors;

public static class IntegerOnlyBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(IntegerOnlyBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.RegisterAttached(
            "ErrorMessage", typeof(string), typeof(IntegerOnlyBehavior),
            new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetErrorMessage(DependencyObject element, string? value) =>
        element.SetValue(ErrorMessageProperty, value);

    public static string? GetErrorMessage(DependencyObject element) =>
        (string?)element.GetValue(ErrorMessageProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox)
            return;

        if ((bool)args.NewValue)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.TextChanged      += OnTextChanged;
            DataObject.AddPastingHandler(textBox, OnPasting);
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.TextChanged      -= OnTextChanged;
            DataObject.RemovePastingHandler(textBox, OnPasting);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        bool hasNonDigit = e.Text.Any(character => character is < '0' or > '9');
        if (hasNonDigit)
        {
            SetErrorMessage(textBox, "Only numeric digits (0-9) are allowed.");
            e.Handled = true;
        }
        else
        {
            SetErrorMessage(textBox, null);
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        if (string.IsNullOrEmpty(textBox.Text) || textBox.Text.All(c => c >= '0' && c <= '9'))
        {
            SetErrorMessage(textBox, null);
        }
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        if (!e.DataObject.GetDataPresent(typeof(string)) ||
            ((string)e.DataObject.GetData(typeof(string))!).Any(character => character is < '0' or > '9'))
        {
            SetErrorMessage(textBox, "Only numeric digits (0-9) are allowed.");
            e.CancelCommand();
        }
        else
        {
            SetErrorMessage(textBox, null);
        }
    }
}