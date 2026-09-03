using System.Windows;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class CustomerFormView : Window
{
    public CustomerFormView(CustomerFormViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.SaveCompleted += OnSaveCompleted;
        Closed += (_, _) => vm.SaveCompleted -= OnSaveCompleted;
    }

    private void OnSaveCompleted()
    {
        DialogResult = true;
        Close();
    }
}
