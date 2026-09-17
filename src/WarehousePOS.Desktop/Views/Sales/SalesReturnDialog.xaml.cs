using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WarehousePOS.Application.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public class ReturnItemRowModel : INotifyPropertyChanged
{
    private int _returnQuantity;

    public int ProductId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int ReturnQuantity
    {
        get => _returnQuantity;
        set
        {
            if (value < 0) value = 0;
            if (value > AvailableQuantity) value = AvailableQuantity;
            if (_returnQuantity != value)
            {
                _returnQuantity = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class SalesReturnDialog : Window
{
    private readonly SaleDto _sale;
    private readonly List<ReturnItemRowModel> _rows = new();

    public IReadOnlyList<ReturnItemRequest> ReturnItems { get; private set; } = new List<ReturnItemRequest>();
    public string ReturnReason { get; private set; } = string.Empty;
    public bool RefundCash { get; private set; }

    public SalesReturnDialog(SaleDto sale)
    {
        InitializeComponent();
        _sale = sale;

        string custName = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName;
        InvoiceHeaderInfo.Text = $"Invoice #{sale.Id} ({sale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}) — {custName}";

        foreach (var item in sale.Items)
        {
            int returnable = item.Quantity - item.ReturnedQuantity;
            _rows.Add(new ReturnItemRowModel
            {
                ProductId = item.ProductId,
                DisplayName = $"{item.ProductName} ({item.SKU})",
                SoldQuantity = item.Quantity,
                ReturnedQuantity = item.ReturnedQuantity,
                AvailableQuantity = Math.Max(0, returnable),
                UnitPrice = item.UnitPrice,
                ReturnQuantity = 0
            });
        }

        GridReturnItems.ItemsSource = _rows;
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        var itemsToReturn = _rows
            .Where(r => r.ReturnQuantity > 0)
            .Select(r => new ReturnItemRequest(r.ProductId, r.ReturnQuantity, TxtReason.Text?.Trim()))
            .ToList();

        if (!itemsToReturn.Any())
        {
            MessageBox.Show("Please enter a return quantity greater than 0 for at least one item.", "No Items Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtReason.Text))
        {
            MessageBox.Show("Please enter a return reason or notes for audit tracking.", "Reason Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ReturnItems = itemsToReturn;
        ReturnReason = TxtReason.Text.Trim();
        RefundCash = ChkRefundCash.IsChecked ?? false;

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
