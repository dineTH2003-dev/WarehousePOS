using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class BillHistoryView : Page
{
    private readonly BillHistoryViewModel _vm;

    public BillHistoryView(BillHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.InitializeAsync();
}
