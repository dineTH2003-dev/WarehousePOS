using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehousePOS.Desktop.Behaviors;

public static class ArrowNavigationBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(ArrowNavigationBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.PreviewKeyDown += OnPreviewKeyDown;
            }
            else
            {
                element.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None)
            return;

        var focusedElement = Keyboard.FocusedElement as UIElement;
        if (focusedElement == null) return;

        if (focusedElement is ComboBox comboBox && comboBox.IsDropDownOpen)
            return;

        if (e.Key == Key.Enter)
        {
            if (focusedElement is TextBox tb && tb.AcceptsReturn)
                return;
            if (focusedElement is Button)
                return;

            focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
            return;
        }

        if (focusedElement is TextBox textBox)
        {
            if (e.Key == Key.Down)
            {
                focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (textBox.CaretIndex == 0 && textBox.SelectionLength == 0)
                {
                    focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Right)
            {
                if (textBox.CaretIndex == (textBox.Text?.Length ?? 0) && textBox.SelectionLength == 0)
                {
                    focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
                    e.Handled = true;
                }
            }
        }
        else
        {
            if (e.Key == Key.Down || e.Key == Key.Right)
            {
                focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
            else if (e.Key == Key.Up || e.Key == Key.Left)
            {
                focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                e.Handled = true;
            }
        }
    }
}
