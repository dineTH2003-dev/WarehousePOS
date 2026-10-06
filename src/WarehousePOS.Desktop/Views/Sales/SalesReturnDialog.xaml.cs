using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
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
    public decimal Discount { get; set; }

    public decimal UnitRefund => Math.Round(UnitPrice - (SoldQuantity > 0 ? (Discount / SoldQuantity) : 0), 2, MidpointRounding.AwayFromZero);
    public decimal LineRefund => UnitRefund * ReturnQuantity;

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
                OnPropertyChanged(nameof(LineRefund));
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
    private bool _isManuallyOverridden;
    private bool _isUpdatingText;

    public IReadOnlyList<ReturnItemRequest> ReturnItems { get; private set; } = new List<ReturnItemRequest>();
    public string ReturnReason { get; private set; } = string.Empty;
    public decimal RefundAmount { get; private set; }
    public bool RefundCash { get; private set; }

    public SalesReturnDialog(SaleDto sale)
    {
        _sale = sale ?? throw new ArgumentNullException(nameof(sale));
        InitializeComponent();

        string custName = string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName;
        InvoiceHeaderInfo.Text = $"Invoice #{sale.Id} ({sale.SaleDate.ToLocalTime():yyyy-MM-dd HH:mm}) — {custName}";

        TxtInvoiceTotal.Text = $"Rs. {sale.TotalAmount:N2}";
        TxtInvoicePaid.Text = $"Rs. {sale.AmountPaid:N2}";
        TxtInvoiceDue.Text = $"Rs. {sale.UnpaidAmount:N2}";

        foreach (var item in sale.Items)
        {
            int returnable = item.Quantity - item.ReturnedQuantity;
            var row = new ReturnItemRowModel
            {
                ProductId = item.ProductId,
                DisplayName = $"{item.ProductName} ({item.SKU})",
                SoldQuantity = item.Quantity,
                ReturnedQuantity = item.ReturnedQuantity,
                AvailableQuantity = Math.Max(0, returnable),
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                ReturnQuantity = 0
            };
            row.PropertyChanged += OnRowPropertyChanged;
            _rows.Add(row);
        }

        GridReturnItems.ItemsSource = _rows;
        UpdateCalculations();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReturnItemRowModel.ReturnQuantity))
        {
            UpdateCalculations();
        }
    }

    private void UpdateCalculations()
    {
        if (TxtCalculatedRefund is null || TxtRefundAmount is null) return;

        decimal calculated = _rows.Sum(r => r.LineRefund);
        TxtCalculatedRefund.Text = $"Rs. {calculated:N2}";

        if (!_isManuallyOverridden)
        {
            _isUpdatingText = true;
            TxtRefundAmount.Text = calculated.ToString("F2");
            _isUpdatingText = false;
        }

        UpdateFinancialImpact();
    }

    private void UpdateFinancialImpact()
    {
        if (_sale is null || TxtRefundAmount is null || TxtDebtOffset is null || TxtCashRefund is null || TxtNewBalanceDue is null || ChkRefundCash is null)
        {
            return;
        }

        if (!decimal.TryParse(TxtRefundAmount.Text, out var refundVal) || refundVal < 0)
        {
            refundVal = 0m;
        }

        decimal debtOffset = Math.Min(refundVal, _sale.UnpaidAmount);
        decimal remainingRefund = refundVal - debtOffset;
        decimal cashPayout = (ChkRefundCash.IsChecked == true && remainingRefund > 0) ? remainingRefund : 0m;
        decimal newDue = Math.Max(0m, _sale.UnpaidAmount - debtOffset);

        TxtDebtOffset.Text = $"Rs. {debtOffset:N2}";
        TxtCashRefund.Text = $"Rs. {cashPayout:N2}";
        TxtNewBalanceDue.Text = $"Rs. {newDue:N2}";
    }

    private void TxtRefundAmount_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText || _sale is null) return;
        _isManuallyOverridden = true;
        UpdateFinancialImpact();
    }

    private void BtnResetRefund_Click(object sender, RoutedEventArgs e)
    {
        _isManuallyOverridden = false;
        decimal calculated = _rows.Sum(r => r.LineRefund);
        _isUpdatingText = true;
        if (TxtRefundAmount is not null)
        {
            TxtRefundAmount.Text = calculated.ToString("F2");
        }
        _isUpdatingText = false;
        UpdateFinancialImpact();
    }

    private void ChkRefundCash_CheckedChanged(object sender, RoutedEventArgs e)
    {
        UpdateFinancialImpact();
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        var itemsToReturn = _rows
            .Where(r => r.ReturnQuantity > 0)
            .Select(r => new ReturnItemRequest(r.ProductId, r.ReturnQuantity, TxtReason?.Text?.Trim()))
            .ToList();

        if (!itemsToReturn.Any())
        {
            MessageBox.Show("Please enter a return quantity greater than 0 for at least one item.", "No Items Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtReason?.Text))
        {
            MessageBox.Show("Please enter a return reason or notes for audit tracking.", "Reason Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtRefundAmount is null || !decimal.TryParse(TxtRefundAmount.Text, out var refundAmount) || refundAmount < 0)
        {
            MessageBox.Show("Please enter a valid refund amount (0.00 or greater).", "Invalid Refund Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (refundAmount > _sale.TotalAmount)
        {
            var res = MessageBox.Show($"Entered refund amount (Rs. {refundAmount:N2}) exceeds the original invoice total (Rs. {_sale.TotalAmount:N2}). Do you want to continue?", "High Refund Amount", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes) return;
        }

        ReturnItems = itemsToReturn;
        ReturnReason = TxtReason?.Text?.Trim() ?? string.Empty;
        RefundAmount = refundAmount;
        RefundCash = ChkRefundCash?.IsChecked ?? false;

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
