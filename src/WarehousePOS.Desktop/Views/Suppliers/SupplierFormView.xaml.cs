using System.Windows;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierFormView : Window
{
    public SupplierFormView(SupplierFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveCompleted += () => { DialogResult = true; Close(); };
    }
}
