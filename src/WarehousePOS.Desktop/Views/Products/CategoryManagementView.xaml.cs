using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class CategoryManagementView : Page
{
    private readonly CategoryManagementViewModel _vm;

    public CategoryManagementView(CategoryManagementViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.LoadAsync();
}
