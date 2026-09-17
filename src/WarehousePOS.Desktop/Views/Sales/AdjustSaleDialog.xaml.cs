using System.Globalization;
using System.Windows;
using WarehousePOS.Application.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class AdjustSaleDialog : Window
{
    private readonly SaleDto _sale;

    public AdjustSaleRequest? AdjustmentRequest { get; private set; }

    public AdjustSaleDialog(SaleDto sale)
    {
        InitializeComponent();
        _sale = sale;

        InvoiceHeaderInfo.Text = $"Invoice #{sale.Id} ({sale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm})";
        TxtCustomerName.Text = sale.CustomerName ?? string.Empty;
        TxtCustomerPhone.Text = sale.CustomerPhone ?? string.Empty;
        TxtDeliveryAddress.Text = sale.DeliveryAddress ?? string.Empty;
        TxtDeliveryFee.Text = sale.DeliveryFee.ToString("F2", CultureInfo.InvariantCulture);
        TxtNotes.Text = sale.Notes ?? string.Empty;
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        decimal deliveryFee = 0;
        if (!string.IsNullOrWhiteSpace(TxtDeliveryFee.Text))
        {
            if (!decimal.TryParse(TxtDeliveryFee.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out deliveryFee) &&
                !decimal.TryParse(TxtDeliveryFee.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out deliveryFee))
            {
                MessageBox.Show("Please enter a valid numeric delivery fee.", "Invalid Delivery Fee", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        AdjustmentRequest = new AdjustSaleRequest(
            _sale.Id,
            1, // Assigned by caller
            string.IsNullOrWhiteSpace(TxtNotes.Text) ? "Bill details amended" : TxtNotes.Text.Trim(),
            deliveryFee,
            string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim(),
            string.IsNullOrWhiteSpace(TxtCustomerName.Text) ? null : TxtCustomerName.Text.Trim(),
            string.IsNullOrWhiteSpace(TxtCustomerPhone.Text) ? null : TxtCustomerPhone.Text.Trim(),
            string.IsNullOrWhiteSpace(TxtDeliveryAddress.Text) ? null : TxtDeliveryAddress.Text.Trim());

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
