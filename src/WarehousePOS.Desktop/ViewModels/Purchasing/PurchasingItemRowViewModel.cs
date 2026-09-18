using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Purchasing;

public sealed class PurchasingItemRowViewModel : ViewModelBase
{
    private ProductDto? _product;
    private int _quantity = 0;
    private int _freeQuantity = 0;
    private int _claimedQuantityReceived = 0;
    private string _unitCostText = "0.00";
    private string _discountRateText = "0.00";
    private string _totalCostText = "0.00";
    private string _retailPriceText = "0.00";
    private string _wholesalePriceText = "0.00";
    private bool _isUpdatingCost;
    private string _lastEditedField = "UnitCost";

    public ProductDto? Product
    {
        get => _product;
        set
        {
            if (SetField(ref _product, value))
            {
                if (value is not null)
                {
                    RetailPriceText = value.RetailPrice.ToString("F2");
                    WholesalePriceText = value.WholesalePrice.ToString("F2");
                    RecalculateCostsOnQuantityChange();
                }
                OnPropertyChanged(nameof(ProductError));
                OnPropertyChanged(nameof(HasPendingClaim));
                OnPropertyChanged(nameof(ClaimedText));
                OnPropertyChanged(nameof(ClaimedQtyError));
            }
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 0) value = 0;
            if (SetField(ref _quantity, value))
            {
                RecalculateCostsOnQuantityChange();
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(QuantityError));
            }
        }
    }

    public string? ProductError   => Product == null ? "Product is required." : null;
    public string? QuantityError  => null;
    public string? ClaimedQtyError => ClaimedQuantityReceived > (Product?.ClaimedQuantity ?? 0) ? $"Max claim: {Product?.ClaimedQuantity ?? 0}." : null;
    public string? UnitCostError  => (Quantity > 0 && UnitCost <= 0) ? "Unit cost must be > 0." : null;

    public bool HasPendingClaim => Product != null && Product.ClaimedQuantity > 0;
    public string ClaimedText => Product != null && Product.ClaimedQuantity > 0 ? $"Claimed Red: {Product.ClaimedQuantity}" : string.Empty;

    public int FreeQuantity
    {
        get => _freeQuantity;
        set
        {
            if (value < 0) value = 0;
            if (SetField(ref _freeQuantity, value))
            {
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(QuantityError));
            }
        }
    }

    public int ClaimedQuantityReceived
    {
        get => _claimedQuantityReceived;
        set
        {
            if (value < 0) value = 0;
            if (SetField(ref _claimedQuantityReceived, value))
            {
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(QuantityError));
                OnPropertyChanged(nameof(ClaimedQtyError));
            }
        }
    }

    public int TotalQuantity => Quantity + FreeQuantity + ClaimedQuantityReceived;

    public string UnitCostText
    {
        get => _unitCostText;
        set
        {
            if (SetField(ref _unitCostText, value) && !_isUpdatingCost)
            {
                OnUnitCostChanged();
            }
        }
    }

    public string DiscountRateText
    {
        get => _discountRateText;
        set
        {
            if (SetField(ref _discountRateText, value) && !_isUpdatingCost)
            {
                OnDiscountRateChanged();
            }
        }
    }

    public string TotalCostText
    {
        get => _totalCostText;
        set
        {
            if (SetField(ref _totalCostText, value) && !_isUpdatingCost)
            {
                OnTotalCostChanged();
            }
        }
    }

    public string RetailPriceText
    {
        get => _retailPriceText;
        set => SetField(ref _retailPriceText, value);
    }

    public string WholesalePriceText
    {
        get => _wholesalePriceText;
        set => SetField(ref _wholesalePriceText, value);
    }

    public decimal UnitCost => decimal.TryParse(UnitCostText, out var cost) && cost >= 0 ? cost : 0;
    public decimal DiscountRate => decimal.TryParse(DiscountRateText, out var rate) && rate >= 0 ? rate : 0;
    public decimal TotalCost => decimal.TryParse(TotalCostText, out var total) && total >= 0 ? total : 0;
    public decimal RetailPrice => decimal.TryParse(RetailPriceText, out var retail) && retail >= 0 ? retail : 0;
    public decimal WholesalePrice => decimal.TryParse(WholesalePriceText, out var wholesale) && wholesale >= 0 ? wholesale : 0;

    public decimal BaseGrossCost => Quantity * UnitCost;
    public decimal LineDiscountAmount => Math.Max(0m, BaseGrossCost - TotalCost);

    public RelayCommand RemoveCommand { get; }

    public event Action<PurchasingItemRowViewModel>? RemoveRequested;

    public PurchasingItemRowViewModel()
    {
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
    }

    private void NotifyAllCostFields()
    {
        OnPropertyChanged(nameof(UnitCost));
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(DiscountRate));
        OnPropertyChanged(nameof(BaseGrossCost));
        OnPropertyChanged(nameof(LineDiscountAmount));
        OnPropertyChanged(nameof(UnitCostError));
    }

    private void OnUnitCostChanged()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            var u = UnitCost;
            var d = DiscountRate;
            var total = Quantity * u * (1m - d / 100m);
            _totalCostText = Math.Max(0m, total).ToString("F2");
            OnPropertyChanged(nameof(TotalCostText));
            _lastEditedField = "UnitCost";
        }
        finally
        {
            _isUpdatingCost = false;
        }

        NotifyAllCostFields();
    }

    private void OnDiscountRateChanged()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            var d = DiscountRate;
            var u = UnitCost;
            var t = TotalCost;

            if (_lastEditedField == "TotalCost" && t > 0 && Quantity > 0 && d < 100m)
            {
                var unit = t / (Quantity * (1m - d / 100m));
                _unitCostText = Math.Max(0m, unit).ToString("F2");
                OnPropertyChanged(nameof(UnitCostText));
            }
            else
            {
                var total = Quantity * u * (1m - d / 100m);
                _totalCostText = Math.Max(0m, total).ToString("F2");
                OnPropertyChanged(nameof(TotalCostText));
            }
            _lastEditedField = "DiscountRate";
        }
        finally
        {
            _isUpdatingCost = false;
        }

        NotifyAllCostFields();
    }

    private void OnTotalCostChanged()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            var t = TotalCost;
            var u = UnitCost;
            var d = DiscountRate;

            if (_lastEditedField == "DiscountRate" && d > 0 && Quantity > 0 && d < 100m)
            {
                var unit = t / (Quantity * (1m - d / 100m));
                _unitCostText = Math.Max(0m, unit).ToString("F2");
                OnPropertyChanged(nameof(UnitCostText));
            }
            else if (Quantity > 0 && u > 0)
            {
                var gross = Quantity * u;
                if (gross > 0)
                {
                    var rate = (1m - t / gross) * 100m;
                    _discountRateText = Math.Max(0m, rate).ToString("F2");
                    OnPropertyChanged(nameof(DiscountRateText));
                }
            }
            else if (Quantity > 0 && t > 0 && u == 0)
            {
                var unit = t / Quantity;
                _unitCostText = Math.Max(0m, unit).ToString("F2");
                OnPropertyChanged(nameof(UnitCostText));
            }
            _lastEditedField = "TotalCost";
        }
        finally
        {
            _isUpdatingCost = false;
        }

        NotifyAllCostFields();
    }

    private void RecalculateCostsOnQuantityChange()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            var u = UnitCost;
            var d = DiscountRate;
            var total = Quantity * u * (1m - d / 100m);
            _totalCostText = Math.Max(0m, total).ToString("F2");
            OnPropertyChanged(nameof(TotalCostText));
        }
        finally
        {
            _isUpdatingCost = false;
        }

        NotifyAllCostFields();
    }
}
