using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Purchasing;

public sealed class PurchasingItemRowViewModel : ViewModelBase
{
    private ProductDto? _product;
    private int _quantity = 1;
    private int _freeQuantity = 0;
    private string _unitCostText = "0.00";
    private string _totalCostText = "0.00";
    private string _retailPriceText = "0.00";
    private string _wholesalePriceText = "0.00";
    private bool _isUpdatingCost;

    public ProductDto? Product
    {
        get => _product;
        set
        {
            if (SetField(ref _product, value) && value is not null)
            {
                RetailPriceText = value.RetailPrice.ToString("F2");
                WholesalePriceText = value.WholesalePrice.ToString("F2");
                RecalculateTotalCost();
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
                RecalculateTotalCost();
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }
    }

    public int FreeQuantity
    {
        get => _freeQuantity;
        set
        {
            if (value < 0) value = 0;
            if (SetField(ref _freeQuantity, value))
            {
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }
    }

    public int TotalQuantity => Quantity + FreeQuantity;

    public string UnitCostText
    {
        get => _unitCostText;
        set
        {
            if (SetField(ref _unitCostText, value) && !_isUpdatingCost)
            {
                RecalculateTotalCost();
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
                RecalculateUnitCost();
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
    public decimal TotalCost => decimal.TryParse(TotalCostText, out var total) && total >= 0 ? total : 0;
    public decimal RetailPrice => decimal.TryParse(RetailPriceText, out var retail) && retail >= 0 ? retail : 0;
    public decimal WholesalePrice => decimal.TryParse(WholesalePriceText, out var wholesale) && wholesale >= 0 ? wholesale : 0;

    public RelayCommand RemoveCommand { get; }

    public event Action<PurchasingItemRowViewModel>? RemoveRequested;

    public PurchasingItemRowViewModel()
    {
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this));
    }

    private void RecalculateTotalCost()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            if (decimal.TryParse(UnitCostText, out var unitCost) && unitCost >= 0)
            {
                var total = Quantity * unitCost;
                TotalCostText = total.ToString("F2");
            }
        }
        finally
        {
            _isUpdatingCost = false;
        }

        OnPropertyChanged(nameof(UnitCost));
        OnPropertyChanged(nameof(TotalCost));
    }

    private void RecalculateUnitCost()
    {
        if (_isUpdatingCost) return;
        _isUpdatingCost = true;

        try
        {
            if (decimal.TryParse(TotalCostText, out var totalCost) && totalCost >= 0 && Quantity > 0)
            {
                var unit = totalCost / Quantity;
                UnitCostText = unit.ToString("F2");
            }
        }
        finally
        {
            _isUpdatingCost = false;
        }

        OnPropertyChanged(nameof(UnitCost));
        OnPropertyChanged(nameof(TotalCost));
    }
}
