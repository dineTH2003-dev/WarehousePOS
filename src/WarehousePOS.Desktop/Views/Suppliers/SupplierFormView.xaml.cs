using System.Windows;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierFormView : Window
{
    public SupplierFormView(SupplierFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveCompleted += OnSaveCompleted;
        Closed += (_, _) => vm.SaveCompleted -= OnSaveCompleted;
    }

    private void OnSaveCompleted()
    {
        DialogResult = true;
    }
}
