using System.Windows;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ProductFormView : Window
{
    public ProductFormView(ProductFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveCompleted += () => { DialogResult = true; Close(); };
    }
}
