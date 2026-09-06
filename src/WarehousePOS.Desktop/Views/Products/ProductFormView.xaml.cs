using System.Windows;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ProductFormView : Window
{
    private readonly ProductFormViewModel _vm;
    private readonly ICategoryService _categoryService;

    public bool NavigateToCategoriesRequested { get; private set; }

    public ProductFormView(ProductFormViewModel vm, ICategoryService categoryService)
    {
        InitializeComponent();
        _vm = vm;
        _categoryService = categoryService;
        DataContext = vm;

        vm.SaveCompleted += OnSaveCompleted;
        vm.AddCategoryRequested += OnAddCategoryRequested;
        vm.ManageCategoriesRequested += OnManageCategoriesRequested;

        Closed += (_, _) =>
        {
            vm.SaveCompleted -= OnSaveCompleted;
            vm.AddCategoryRequested -= OnAddCategoryRequested;
            vm.ManageCategoriesRequested -= OnManageCategoriesRequested;
        };
    }

    private void OnSaveCompleted()
    {
        DialogResult = true;
    }

    private async void OnAddCategoryRequested()
    {
        var dlg = new CategoryQuickAddDialog(_categoryService) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.CreatedCategory is not null)
        {
            await _vm.RefreshCategoriesAsync(dlg.CreatedCategory.Id);
        }
        else if (dlg.NavigateToFullPageRequested)
        {
            NavigateToCategoriesRequested = true;
            DialogResult = false;
            Close();
        }
    }

    private void OnManageCategoriesRequested()
    {
        NavigateToCategoriesRequested = true;
        DialogResult = false;
        Close();
    }
}

