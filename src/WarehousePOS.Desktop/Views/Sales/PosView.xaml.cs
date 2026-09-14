using System.Windows.Controls;
using System.Windows.Input;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class PosView : Page
{
    private readonly PosViewModel _vm;

    public PosView(PosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.InitializeAsync();

    private void TextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            textBox.Focus();
        }
    }

    private void TxtCustomerSearch_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null || !_vm.IsCustomerDropDownOpen || LstCustomerResults == null)
            return;

        if (e.Key == Key.Down)
        {
            if (LstCustomerResults.Items.Count > 0)
            {
                if (LstCustomerResults.SelectedIndex < LstCustomerResults.Items.Count - 1)
                {
                    LstCustomerResults.SelectedIndex++;
                }
                else if (LstCustomerResults.SelectedIndex < 0)
                {
                    LstCustomerResults.SelectedIndex = 0;
                }
                LstCustomerResults.ScrollIntoView(LstCustomerResults.SelectedItem);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (LstCustomerResults.Items.Count > 0)
            {
                if (LstCustomerResults.SelectedIndex > 0)
                {
                    LstCustomerResults.SelectedIndex--;
                    LstCustomerResults.ScrollIntoView(LstCustomerResults.SelectedItem);
                }
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (LstCustomerResults.SelectedItem is WarehousePOS.Application.Sales.CustomerDto customer)
            {
                CommitCustomerSelection(customer);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            _vm.IsCustomerDropDownOpen = false;
            e.Handled = true;
        }
    }

    private void LstCustomerResults_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (LstCustomerResults?.SelectedItem is WarehousePOS.Application.Sales.CustomerDto customer)
        {
            CommitCustomerSelection(customer);
        }
    }

    private void CommitCustomerSelection(WarehousePOS.Application.Sales.CustomerDto customer)
    {
        if (_vm != null)
        {
            _vm.SelectedCustomer = customer;
            _vm.IsCustomerDropDownOpen = false;
        }
    }
}
