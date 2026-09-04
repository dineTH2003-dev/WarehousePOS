using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WarehousePOS.Desktop.ViewModels.Expenses;

namespace WarehousePOS.Desktop.Views.Expenses;

public partial class ExpenseListView : Page
{
    private readonly ExpenseListViewModel _vm;

    public ExpenseListView(ExpenseListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.LoadDataAsync();

    private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            string newText = GetProposedText(textBox, e.Text);
            // Allow typing digits and at most one decimal point with up to 2 decimal places
            e.Handled = !IsPartialValidPositiveDecimal(newText);
        }
    }

    private void AmountTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var pasteText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (sender is TextBox textBox)
            {
                string proposedText = GetProposedText(textBox, pasteText);
                if (!IsPartialValidPositiveDecimal(proposedText))
                {
                    e.CancelCommand();
                }
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static string GetProposedText(TextBox textBox, string input)
    {
        string currentText = textBox.Text ?? string.Empty;
        int selectionStart = textBox.SelectionStart;
        int selectionLength = textBox.SelectionLength;

        return currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, input);
    }

    private static bool IsPartialValidPositiveDecimal(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        // Allows typing partial positive numbers, e.g. "1", "1500", "1500.", "1500.5", "1500.50"
        return Regex.IsMatch(text, @"^[0-9]*\.?[0-9]{0,2}$");
    }
}

