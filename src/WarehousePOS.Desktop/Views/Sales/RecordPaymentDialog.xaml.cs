using System.Globalization;
using System.Windows;
using WarehousePOS.Application.Sales;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class RecordPaymentDialog : Window
{
    private readonly SaleDto _sale;

    public RecordSalePaymentRequest? PaymentRequest { get; private set; }

    public RecordPaymentDialog(SaleDto sale)
    {
        InitializeComponent();
        _sale = sale;

        string custName = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName;
        InvoiceInfoText.Text = $"Invoice #{sale.Id} — {custName}";

        decimal unpaid = sale.UnpaidAmount > 0 ? sale.UnpaidAmount : Math.Max(0, sale.TotalAmount - sale.AmountPaid);
        TxtBillTotal.Text = $"Rs. {sale.TotalAmount:N2}";
        TxtAmountPaid.Text = $"Rs. {sale.AmountPaid:N2}";
        TxtBalanceDue.Text = $"Rs. {unpaid:N2}";

        TxtPaymentAmount.Text = unpaid.ToString("F2", CultureInfo.InvariantCulture);

        CmbPaymentMethod.ItemsSource = new[]
        {
            PaymentMethod.Cash,
            PaymentMethod.Card,
            PaymentMethod.BankTransfer,
            PaymentMethod.Cheque
        };
        CmbPaymentMethod.SelectedItem = PaymentMethod.Cash;
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtPaymentAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
            !decimal.TryParse(TxtPaymentAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
        {
            MessageBox.Show("Please enter a valid payment amount.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (amount <= 0)
        {
            MessageBox.Show("Payment amount must be greater than zero.", "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var method = (PaymentMethod)(CmbPaymentMethod.SelectedItem ?? PaymentMethod.Cash);
        var notes = TxtNotes.Text?.Trim();

        PaymentRequest = new RecordSalePaymentRequest(
            _sale.Id,
            amount,
            method,
            1, // Cashier ID will be assigned by caller
            notes);

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
