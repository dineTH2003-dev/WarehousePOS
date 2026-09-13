using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

        // Never intercept keyboard navigation inside tables/grids/lists
        if (IsInsideDataGridOrList(focusedElement))
            return;

        // Never intercept dropdown/selection in ComboBox
        if (focusedElement is ComboBox comboBox)
        {
            if (comboBox.IsDropDownOpen || e.Key == Key.Up || e.Key == Key.Down)
                return;
        }

        // Never intercept DatePicker when popup is open
        if (focusedElement is DatePicker datePicker && datePicker.IsDropDownOpen)
            return;

        // Never intercept Calendar date picking controls
        if (focusedElement is Calendar || focusedElement is CalendarDayButton || focusedElement is CalendarButton || focusedElement is CalendarItem)
            return;

        // Never intercept TabItem header switching
        if (focusedElement is TabItem)
            return;

        // Never intercept Slider value changes
        if (focusedElement is Slider)
            return;

        // Never intercept RadioButton group navigation
        if (focusedElement is RadioButton)
        {
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
                return;
        }

        // Enter key moves focus to next logical element
        if (e.Key == Key.Enter)
        {
            if (focusedElement is TextBox tb && tb.AcceptsReturn)
                return;
            if (focusedElement is Button)
                return;
            if (focusedElement is ComboBox cb && cb.IsDropDownOpen)
                return;

            focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
            return;
        }

        if (focusedElement is TextBox textBox)
        {
            // For multiline textboxes, preserve Up/Down arrow line navigation
            if (textBox.AcceptsReturn)
                return;

            if (e.Key == Key.Down)
            {
                MoveDown(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveUp(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                if (textBox.CaretIndex == 0 && textBox.SelectionLength == 0)
                {
                    MoveLeft(focusedElement);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Right)
            {
                if (textBox.CaretIndex == (textBox.Text?.Length ?? 0) && textBox.SelectionLength == 0)
                {
                    MoveRight(focusedElement);
                    e.Handled = true;
                }
            }
        }
        else if (focusedElement is PasswordBox)
        {
            if (e.Key == Key.Down)
            {
                MoveDown(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveUp(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                MoveRight(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                MoveLeft(focusedElement);
                e.Handled = true;
            }
        }
        else
        {
            if (e.Key == Key.Down)
            {
                MoveDown(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveUp(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                MoveRight(focusedElement);
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                MoveLeft(focusedElement);
                e.Handled = true;
            }
        }
    }

    private static void MoveDown(UIElement element)
    {
        if (!element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down)))
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }

    private static void MoveUp(UIElement element)
    {
        if (!element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up)))
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
    }

    private static void MoveRight(UIElement element)
    {
        if (!element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right)))
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
    }

    private static void MoveLeft(UIElement element)
    {
        if (!element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left)))
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
    }

    private static bool IsInsideDataGridOrList(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is DataGrid || element is DataGridCell || element is DataGridRow ||
                element is ListBox || element is ListBoxItem)
            {
                return true;
            }

            if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                element = VisualTreeHelper.GetParent(element);
            else
                element = LogicalTreeHelper.GetParent(element);
        }
        return false;
    }
}
