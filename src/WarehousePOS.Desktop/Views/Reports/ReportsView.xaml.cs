using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Reports;

namespace WarehousePOS.Desktop.Views.Reports;

public partial class ReportsView : Page
{
    private readonly ReportsViewModel _vm;

    public ReportsView(ReportsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.LoadAllReportsAsync();

    private void QuickThisMonth_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SelectedPreset = WarehousePOS.Application.Reports.DateRangePreset.ThisMonth;
    }

    private void QuickLast30Days_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SelectedPreset = WarehousePOS.Application.Reports.DateRangePreset.Last30Days;
    }
}
