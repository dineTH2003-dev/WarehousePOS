using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class CategoryManagementViewModel : ViewModelBase
{
    private readonly ICategoryService   _categoryService;
    private readonly INavigationService _nav;
    private readonly SessionContext    _session;

    private ObservableCollection<CategoryDto> _categories = [];
    private CategoryDto? _selectedCategory;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isEditing;

    public ObservableCollection<CategoryDto> Categories
    {
        get => _categories;
        private set => SetField(ref _categories, value);
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            SetField(ref _selectedCategory, value);
            if (value is not null) LoadForEdit(value);
        }
    }

    public string Name           { get => _name;           set => SetField(ref _name, value); }
    public string Description    { get => _description;    set => SetField(ref _description, value); }
    public string ErrorMessage   { get => _errorMessage;   set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public string SuccessMessage { get => _successMessage; set { SetField(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccessMessage)); } }
    public bool HasError          => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);
    public bool IsEditing        { get => _isEditing;      set { SetField(ref _isEditing, value); OnPropertyChanged(nameof(SaveButtonText)); } }
    public string SaveButtonText => IsEditing ? "Update Category" : "+ Add Category";
    public bool IsAdmin          => _session.IsAdmin;

    public RelayCommand SaveCommand       { get; }
    public RelayCommand CancelCommand     { get; }
    public RelayCommand BackCommand       { get; }
    public RelayCommand AddProductCommand { get; }
    public RelayCommand<CategoryDto> ToggleActiveCommand { get; }

    public CategoryManagementViewModel(
        ICategoryService categoryService,
        INavigationService nav,
        SessionContext session)
    {
        _categoryService = categoryService;
        _nav             = nav;
        _session         = session;

        SaveCommand         = new RelayCommand(async () => await SaveAsync());
        CancelCommand       = new RelayCommand(ClearForm);
        BackCommand         = new RelayCommand(() => _nav.NavigateTo<ProductListViewModel>());
        AddProductCommand   = new RelayCommand(() =>
        {
            ProductListViewModel.PendingOpenAddProduct = true;
            _nav.NavigateTo<ProductListViewModel>();
        });
        ToggleActiveCommand = new RelayCommand<CategoryDto>(async dto => await ToggleActiveAsync(dto));
    }

    public async Task LoadAsync()
    {
        var cats = await _categoryService.GetAllAsync();
        Categories = new ObservableCollection<CategoryDto>(cats);
    }

    private void LoadForEdit(CategoryDto dto)
    {
        IsEditing = true;
        Name = dto.Name;
        Description = dto.Description ?? string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Category name is required."; return; }
        try
        {
            var savedName = Name.Trim();
            if (IsEditing && SelectedCategory is not null)
            {
                await _categoryService.UpdateAsync(new UpdateCategoryRequest(SelectedCategory.Id, savedName, Description));
                SuccessMessage = $"Category '{savedName}' updated successfully.";
            }
            else
            {
                await _categoryService.CreateAsync(new CreateCategoryRequest(savedName, Description));
                SuccessMessage = $"Category '{savedName}' added successfully.";
            }

            ClearForm();
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private async Task ToggleActiveAsync(CategoryDto? dto)
    {
        if (dto is null) return;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (dto.IsActive) await _categoryService.DeactivateAsync(dto.Id);
        else await _categoryService.ActivateAsync(dto.Id);
        await LoadAsync();
    }

    private void ClearForm()
    {
        IsEditing = false;
        SelectedCategory = null;
        Name = string.Empty;
        Description = string.Empty;
        ErrorMessage = string.Empty;
    }
}
