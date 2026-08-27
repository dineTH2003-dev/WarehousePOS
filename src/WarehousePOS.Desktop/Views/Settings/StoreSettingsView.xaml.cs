using System.Windows.Controls;
using WarehousePOS.Desktop.ViewModels.Settings;

namespace WarehousePOS.Desktop.Views.Settings;

public partial class StoreSettingsView : Page
{
    private readonly StoreSettingsViewModel _vm;

    public StoreSettingsView(StoreSettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public async Task InitAsync() => await _vm.LoadSettingsAsync();
}
