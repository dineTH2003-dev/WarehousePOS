using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Desktop.Services;

namespace WarehousePOS.Desktop.ViewModels.Expenses;

public sealed class ExpenseListViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly SessionContext _sessionContext;

    private ObservableCollection<ExpenseDto> _expenses = [];
    private ObservableCollection<ExpenseCategoryDto> _categories = [];

    // Form inputs
    private int _selectedCategoryId;
    private string _amountText = string.Empty;
    private string _description = string.Empty;
    private string _referenceNo = string.Empty;
    private string _errorMessage = string.Empty;
    private string _amountError = string.Empty;
    private bool _isBusy;

    public ObservableCollection<ExpenseDto> Expenses => _expenses;
    public ObservableCollection<ExpenseCategoryDto> Categories => _categories;

    public int SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (SetField(ref _selectedCategoryId, value))
            {
                ClearGeneralError();
                AddExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AmountText
    {
        get => _amountText;
        set
        {
            if (SetField(ref _amountText, value))
            {
                ValidateAmountInput(value);
                ClearGeneralError();
                AddExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetField(ref _description, value))
            {
                ClearGeneralError();
                AddExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ReferenceNo
    {
        get => _referenceNo;
        set => SetField(ref _referenceNo, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetField(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string AmountError
    {
        get => _amountError;
        set
        {
            if (SetField(ref _amountError, value))
                OnPropertyChanged(nameof(HasAmountError));
        }
    }

    public bool HasAmountError => !string.IsNullOrEmpty(AmountError);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
                AddExpenseCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal TotalExpensesSum => _expenses.Sum(e => e.Amount);

    public RelayCommand AddExpenseCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public ExpenseListViewModel(IExpenseService expenseService, SessionContext sessionContext)
    {
        _expenseService = expenseService;
        _sessionContext = sessionContext;

        AddExpenseCommand = new RelayCommand(async () => await AddExpenseAsync(), () => CanSubmitExpense());
        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var cats = await _expenseService.GetCategoriesAsync();
            _categories.Clear();
            foreach (var c in cats.Where(x => x.IsActive))
                _categories.Add(c);

            if (_categories.Count > 0)
            {
                if (_selectedCategoryId <= 0 || !_categories.Any(c => c.Id == _selectedCategoryId))
                    SelectedCategoryId = _categories[0].Id;
            }
            else
            {
                SelectedCategoryId = 0;
            }

            var list = await _expenseService.GetAllAsync();
            _expenses.Clear();
            foreach (var item in list)
                _expenses.Add(item);

            OnPropertyChanged(nameof(TotalExpensesSum));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load expenses: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ValidateAmountInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            AmountError = string.Empty;
            return;
        }

        var trimmed = text.Trim();

        // Must match positive decimal format (digits, optional dot, at most 2 decimal places)
        if (!Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]+)?$"))
        {
            AmountError = "Amount contains invalid characters. Use positive digits only (e.g. 1500.00).";
            return;
        }

        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) &&
            !decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out amount))
        {
            AmountError = "Enter a valid numeric amount.";
            return;
        }

        if (amount <= 0)
        {
            AmountError = "Amount must be greater than 0.";
            return;
        }

        var dotIndex = trimmed.IndexOf('.');
        if (dotIndex >= 0 && trimmed.Length - dotIndex - 1 > 2)
        {
            AmountError = "Amount cannot have more than 2 decimal places.";
            return;
        }

        AmountError = string.Empty;
    }

    public bool CanSubmitExpense()
    {
        if (IsBusy) return false;
        if (SelectedCategoryId <= 0) return false;
        if (string.IsNullOrWhiteSpace(AmountText)) return false;
        if (HasAmountError) return false;
        if (!TryParsePositiveAmount(AmountText, out _)) return false;
        if (string.IsNullOrWhiteSpace(Description)) return false;

        return true;
    }

    private static bool TryParsePositiveAmount(string text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();

        if (!Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]{1,2})?$"))
            return false;

        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) &&
            !decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out amount))
        {
            return false;
        }

        return amount > 0;
    }

    private async Task AddExpenseAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedCategoryId <= 0)
        {
            ErrorMessage = _categories.Count == 0
                ? "No expense categories available. Please configure expense categories first."
                : "Please select an expense category.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AmountText))
        {
            AmountError = "Amount is required.";
            ErrorMessage = "Please enter an amount.";
            return;
        }

        if (!TryParsePositiveAmount(AmountText, out var amount))
        {
            ValidateAmountInput(AmountText);
            if (string.IsNullOrEmpty(AmountError))
                AmountError = "Enter a valid positive expense amount (e.g. 1500.00).";
            ErrorMessage = AmountError;
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Description is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var userId = _sessionContext.CurrentUser?.UserId ?? 1;
            var req = new CreateExpenseRequest(
                SelectedCategoryId,
                amount,
                Description.Trim(),
                userId,
                DateTime.UtcNow,
                string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo.Trim());

            await _expenseService.CreateAsync(req);

            // Reset form fields
            AmountText = string.Empty;
            AmountError = string.Empty;
            Description = string.Empty;
            ReferenceNo = string.Empty;
            ErrorMessage = string.Empty;

            if (_categories.Count > 0)
                SelectedCategoryId = _categories[0].Id;

            // Refresh list
            var list = await _expenseService.GetAllAsync();
            _expenses.Clear();
            foreach (var item in list)
                _expenses.Add(item);

            OnPropertyChanged(nameof(TotalExpensesSum));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save expense: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearGeneralError()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }
}

