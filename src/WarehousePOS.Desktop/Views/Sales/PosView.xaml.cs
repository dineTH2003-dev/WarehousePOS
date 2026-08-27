using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class PosView : Page
{
    private readonly PosViewModel _vm;

    public PosView(PosViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.InitializeAsync();
}
