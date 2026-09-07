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

        if (focusedElement is TextBox textBox && textBox.AcceptsReturn && e.Key == Key.Enter)
            return;

        if (e.Key == Key.Down || e.Key == Key.Enter)
        {
            focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            e.Handled = true;
        }
    }
}
