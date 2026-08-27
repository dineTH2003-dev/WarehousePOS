using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ProductListView : Page
{
    private readonly ProductListViewModel _vm;
    private readonly ProductFormViewModel _formVm;

    public ProductListView(ProductListViewModel vm, ProductFormViewModel formVm)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnEditRequested(ProductDto? dto)
    {
        await _formVm.LoadAsync(dto);
        var dialog = new ProductFormView(_formVm) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await _vm.LoadAsync();
    }
}
