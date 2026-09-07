using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePOS.Desktop.Behaviors;

public static class DecimalOnlyBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DecimalOnlyBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBox textBox)
            return;

        if ((bool)args.NewValue)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(textBox, OnPasting);
            textBox.LostFocus += OnLostFocus;
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            textBox.LostFocus -= OnLostFocus;
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        e.Handled = !IsValidInsertion(textBox, e.Text);
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        var textBox = (TextBox)sender;
        if (!e.DataObject.GetDataPresent(typeof(string)) ||
            !IsValidInsertion(textBox, (string)e.DataObject.GetData(typeof(string))!))
        {
            e.CancelCommand();
        }
    }

    private static bool IsValidInsertion(TextBox textBox, string insertedText)
    {
        if (string.IsNullOrEmpty(insertedText)) return true;

        var currentText = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        var newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, insertedText);

        if (string.IsNullOrEmpty(newText)) return true;

        int decimalPointCount = 0;
        foreach (var character in newText)
        {
            if (character == '.')
            {
                decimalPointCount++;
                if (decimalPointCount > 1) return false;
            }
            else if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var text = textBox.Text?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            textBox.Text = "0.00";
            return;
        }

        if (decimal.TryParse(text, out var value) && value >= 0)
        {
            textBox.Text = value.ToString("F2");
        }
        else
        {
            textBox.Text = "0.00";
        }
    }
}
