using System.Windows;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class WarrantyClaimDialog : Window
{
    private readonly CustomerPurchasedItemDisplayModel _item;

    public int ClaimQuantity { get; private set; }
    public string? ClaimNotes { get; private set; }

    public WarrantyClaimDialog(CustomerPurchasedItemDisplayModel item)
    {
        InitializeComponent();
        _item = item;

        ProductInfoText.Text = $"Invoice #{item.InvoiceNumber} — {item.ProductName} ({item.SKU})";
        TxtMaxHint.Text = $"Unclaimed line quantity available: {item.UnclaimedQuantity}";
        TxtQuantity.Text = "1";
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtQuantity.Text?.Trim(), out var qty) || qty <= 0)
        {
            MessageBox.Show("Please enter a valid positive whole number for claim quantity.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (qty > _item.UnclaimedQuantity)
        {
            MessageBox.Show($"Claim quantity cannot exceed available unclaimed quantity ({_item.UnclaimedQuantity}).", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ClaimQuantity = qty;
        ClaimNotes = TxtNotes.Text?.Trim();
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
