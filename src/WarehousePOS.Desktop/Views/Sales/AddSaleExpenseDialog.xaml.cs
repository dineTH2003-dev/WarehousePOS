using System.Globalization;
using System.Windows;
using WarehousePOS.Application.Expenses;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class AddSaleExpenseDialog : Window
{
    private readonly int _saleId;

    public CreateExpenseRequest? CreatedExpense { get; private set; }

    public AddSaleExpenseDialog(int saleId, IReadOnlyList<ExpenseCategoryDto> categories)
    {
        InitializeComponent();
        _saleId = saleId;

        InvoiceHeaderInfo.Text = $"Linked to Invoice #{saleId}";
        CmbCategory.ItemsSource = categories.Where(c => c.IsActive).ToList();
        if (categories.Any(c => c.IsActive))
        {
            CmbCategory.SelectedIndex = 0;
        }
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (CmbCategory.SelectedValue is not int categoryId || categoryId <= 0)
        {
            MessageBox.Show("Please select an expense category.", "Category Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
            !decimal.TryParse(TxtAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
        {
            MessageBox.Show("Please enter a valid expense amount.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (amount <= 0)
        {
            MessageBox.Show("Amount must be greater than zero.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtDescription.Text))
        {
            MessageBox.Show("Please enter a description or reason for this outcome.", "Description Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreatedExpense = new CreateExpenseRequest(
            categoryId,
            amount,
            TxtDescription.Text.Trim(),
            1, // Assigned by caller
            DateTime.UtcNow,
            TxtReference.Text?.Trim(),
            _saleId);

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
