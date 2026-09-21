using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class ProcessCustomerClaimDialog : Window
{
    private readonly IReadOnlyList<ProductDto> _rawProducts;
    private readonly IReadOnlyList<CustomerDto> _customers;
    private readonly List<ProductClaimDisplayItem> _displayProducts;

    public ProcessCustomerClaimRequest? Request { get; private set; }

    public ProcessCustomerClaimDialog(
        IReadOnlyList<ProductDto> products,
        IReadOnlyList<CustomerDto> customers,
        CustomerDto? selectedCustomer = null)
    {
        InitializeComponent();

        _rawProducts = products ?? [];
        _customers = customers ?? [];
        _displayProducts = _rawProducts.Select(p => new ProductClaimDisplayItem(p)).ToList();

        CmbProducts.ItemsSource = _displayProducts;
        CmbCustomers.ItemsSource = _customers;

        if (selectedCustomer != null)
        {
            var match = _customers.FirstOrDefault(c => c.Id == selectedCustomer.Id);
            if (match != null)
                CmbCustomers.SelectedItem = match;
        }
        else if (_customers.Count > 0)
        {
            CmbCustomers.SelectedIndex = 0;
        }

        if (_displayProducts.Count > 0)
        {
            CmbProducts.SelectedIndex = 0;
        }
    }

    private void CmbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDefaultClaimAmount();
    }

    private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateDefaultClaimAmount();
    }

    private void UpdateDefaultClaimAmount()
    {
        if (CmbProducts.SelectedItem is ProductClaimDisplayItem displayItem)
        {
            var p = displayItem.Product;
            int qty = int.TryParse(TxtQuantity.Text?.Trim(), out var q) && q > 0 ? q : 1;
            decimal defaultAmount = p.RetailPrice * qty;
            TxtClaimAmount.Text = defaultAmount.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProducts.SelectedItem is not ProductClaimDisplayItem displayItem)
        {
            MessageBox.Show("Please select a valid product for the claim.", "Product Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var product = displayItem.Product;

        if (!int.TryParse(TxtQuantity.Text?.Trim(), out var qty) || qty <= 0)
        {
            MessageBox.Show("Please enter a valid positive quantity.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtClaimAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var claimAmount) || claimAmount < 0)
        {
            if (!decimal.TryParse(TxtClaimAmount.Text?.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out claimAmount) || claimAmount < 0)
            {
                MessageBox.Show("Please enter a valid non-negative claim amount (Rs.).", "Invalid Claim Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var customer = CmbCustomers.SelectedItem as CustomerDto;
        string reasonType = (CmbReasons.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Customer Claim";
        string extraNotes = TxtNotes.Text?.Trim() ?? "";

        string combinedReason = string.IsNullOrWhiteSpace(extraNotes)
            ? reasonType
            : $"{reasonType} — {extraNotes}";

        Request = new ProcessCustomerClaimRequest(
            ProductId: product.Id,
            CustomerId: customer?.Id,
            SaleId: null,
            Quantity: qty,
            ClaimAmount: claimAmount,
            Reason: combinedReason);

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

public sealed class ProductClaimDisplayItem(ProductDto product)
{
    public ProductDto Product { get; } = product;
    public string DisplayName => $"{Product.Name} ({Product.SKU}) — Rs. {Product.RetailPrice:N2}";
}
