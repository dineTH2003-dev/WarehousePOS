using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Expenses;

namespace WarehousePOS.Desktop.Views.Expenses;

public partial class ExpenseListView : Page
{
    private readonly ExpenseListViewModel _vm;

    public ExpenseListView(ExpenseListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.LoadDataAsync();
}
