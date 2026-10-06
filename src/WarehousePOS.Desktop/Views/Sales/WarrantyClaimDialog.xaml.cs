using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class WarrantyClaimDialog : Window
{
    private readonly CustomerPurchasedItemDisplayModel _item;
    private readonly Brush _defaultBorderBrush;
    private readonly Brush _errorBorderBrush;

    public int ClaimQuantity { get; private set; }
    public string? ClaimNotes { get; private set; }

    public WarrantyClaimDialog(CustomerPurchasedItemDisplayModel item)
    {
        InitializeComponent();
        _item = item;
        _defaultBorderBrush = (Brush)FindResource("BorderBrush");
        _errorBorderBrush = (Brush)FindResource("DangerBrush");

        ProductInfoText.Text = $"Invoice #{item.InvoiceNumber} — {item.ProductName} ({item.SKU})";
        TxtMaxHint.Text = $"Unclaimed line quantity available: {item.UnclaimedQuantity}";
        TxtQuantity.Text = item.UnclaimedQuantity > 0 ? "1" : "0";
        ValidateQuantity();
    }

    private bool ValidateQuantity()
    {
        string text = TxtQuantity.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            ShowError("Please enter a claim quantity.");
            return false;
        }

        if (!int.TryParse(text, out var qty))
        {
            ShowError("Please enter a valid whole number for claim quantity.");
            return false;
        }

        if (qty <= 0)
        {
            ShowError("Claim quantity must be at least 1.");
            return false;
        }

        if (qty > _item.UnclaimedQuantity)
        {
            ShowError($"Claim quantity cannot exceed available limit ({_item.UnclaimedQuantity}).");
            return false;
        }

        ClearError();
        return true;
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
        TxtQuantity.BorderBrush = _errorBorderBrush;
        BtnSubmit.IsEnabled = false;
    }

    private void ClearError()
    {
        TxtError.Text = string.Empty;
        TxtError.Visibility = Visibility.Collapsed;
        TxtQuantity.BorderBrush = _defaultBorderBrush;
        BtnSubmit.IsEnabled = true;
    }

    private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtError == null || BtnSubmit == null) return;
        ValidateQuantity();
    }

    private void TxtQuantity_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ValidateQuantity())
        {
            BtnSubmit_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateQuantity())
            return;

        int.TryParse(TxtQuantity.Text?.Trim(), out var qty);
        ClaimQuantity = qty;
        ClaimNotes = TxtNotes.Text?.Trim();
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
