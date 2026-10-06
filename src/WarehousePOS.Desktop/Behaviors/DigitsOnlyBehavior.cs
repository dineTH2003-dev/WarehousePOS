using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePOS.Desktop.Behaviors;

public static class DigitsOnlyBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DigitsOnlyBehavior),
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
            textBox.PreviewKeyDown           += OnPreviewKeyDown;
            textBox.PreviewTextInput         += OnPreviewTextInput;
            textBox.GotFocus                 += OnGotFocus;
            textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            DataObject.AddPastingHandler(textBox, OnPasting);
        }
        else
        {
            textBox.PreviewKeyDown           -= OnPreviewKeyDown;
            textBox.PreviewTextInput         -= OnPreviewTextInput;
            textBox.GotFocus                 -= OnGotFocus;
            textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            DataObject.RemovePastingHandler(textBox, OnPasting);
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Block space key while typing
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            textBox.Focus();
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !IsValidInsertion((TextBox)sender, e.Text);

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)) ||
            !IsValidInsertion((TextBox)sender, (string)e.DataObject.GetData(typeof(string))!))
        {
            e.CancelCommand();
        }
    }

    private static bool IsValidInsertion(TextBox textBox, string insertedText)
    {
        if (string.IsNullOrEmpty(insertedText))
            return true;

        // 1. Only integers / digits allowed (no spaces, letters, or symbols)
        if (!insertedText.All(character => character is >= '0' and <= '9'))
            return false;

        var currentText = textBox.Text ?? string.Empty;
        var start = Math.Min(textBox.SelectionStart, currentText.Length);
        var length = Math.Min(textBox.SelectionLength, currentText.Length - start);
        var proposedText = currentText.Remove(start, length).Insert(start, insertedText);

        // 2. Length should not be greater than 10 integers
        if (proposedText.Length > 10)
            return false;

        // 3. Start phone number with 0
        if (proposedText.Length > 0 && !proposedText.StartsWith('0'))
            return false;

        return true;
    }
}