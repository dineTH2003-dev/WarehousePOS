using System.Windows;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierEntitlementFormView : Window
{
    public SupplierEntitlementFormView(SupplierEntitlementFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
