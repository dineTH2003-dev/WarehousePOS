using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class CategoryManagementViewModel : ViewModelBase
{
    private readonly ICategoryService _categoryService;

    private ObservableCollection<CategoryDto> _categories = [];
    private CategoryDto? _selectedCategory;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _errorMessage = string.Empty;
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

    public string Name         { get => _name;         set => SetField(ref _name, value); }
    public string Description  { get => _description;  set => SetField(ref _description, value); }
    public string ErrorMessage { get => _errorMessage; set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError       => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsEditing      { get => _isEditing;    set => SetField(ref _isEditing, value); }

    public RelayCommand SaveCommand     { get; }
    public RelayCommand CancelCommand   { get; }
    public RelayCommand<CategoryDto> ToggleActiveCommand { get; }

    public CategoryManagementViewModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
        SaveCommand   = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(ClearForm);
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
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        try
        {
            if (IsEditing && SelectedCategory is not null)
                await _categoryService.UpdateAsync(new UpdateCategoryRequest(SelectedCategory.Id, Name, Description));
            else
                await _categoryService.CreateAsync(new CreateCategoryRequest(Name, Description));

            ClearForm();
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private async Task ToggleActiveAsync(CategoryDto? dto)
    {
        if (dto is null) return;
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
