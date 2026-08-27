using System.Collections.ObjectModel;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Expenses;

public sealed class ExpenseListViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly ISessionContext _sessionContext;

    private ObservableCollection<ExpenseDto> _expenses = [];
    private ObservableCollection<ExpenseCategoryDto> _categories = [];

    // Form inputs
    private int _selectedCategoryId;
    private string _amountText = string.Empty;
    private string _description = string.Empty;
    private string _referenceNo = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public ObservableCollection<ExpenseDto> Expenses => _expenses;
    public ObservableCollection<ExpenseCategoryDto> Categories => _categories;

    public int SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetField(ref _selectedCategoryId, value);
    }

    public string AmountText
    {
        get => _amountText;
        set => SetField(ref _amountText, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public string ReferenceNo
    {
        get => _referenceNo;
        set => SetField(ref _referenceNo, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public decimal TotalExpensesSum => _expenses.Sum(e => e.Amount);

    public RelayCommand AddExpenseCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public ExpenseListViewModel(IExpenseService expenseService, ISessionContext sessionContext)
    {
        _expenseService = expenseService;
        _sessionContext = sessionContext;

        AddExpenseCommand = new RelayCommand(async () => await AddExpenseAsync());
        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var cats = await _expenseService.GetCategoriesAsync();
            _categories.Clear();
            foreach (var c in cats) _categories.Add(c);
            if (_categories.Count > 0) SelectedCategoryId = _categories[0].Id;

            var list = await _expenseService.GetAllAsync();
            _expenses.Clear();
            foreach (var item in list) _expenses.Add(item);

            OnPropertyChanged(nameof(TotalExpensesSum));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddExpenseAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedCategoryId <= 0) { ErrorMessage = "Please select an expense category."; return; }
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0) { ErrorMessage = "Enter a valid positive expense amount."; return; }
        if (string.IsNullOrWhiteSpace(Description)) { ErrorMessage = "Description is required."; return; }

        try
        {
            var req = new CreateExpenseRequest(
                SelectedCategoryId, amount, Description, _sessionContext.CurrentUser?.Id ?? 1, DateTime.UtcNow, ReferenceNo);

            await _expenseService.CreateAsync(req);

            // Reset form
            AmountText = string.Empty;
            Description = string.Empty;
            ReferenceNo = string.Empty;

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
