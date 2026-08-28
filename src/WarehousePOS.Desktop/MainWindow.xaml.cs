using System.Windows;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.ViewModels.Expenses;

namespace WarehousePOS.Desktop;

/// <summary>
/// MainWindow shell — hosts the sidebar nav and the main content Frame.
/// All content lives in Pages navigated via INavigationService.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _nav;

    public MainWindow(INavigationService nav)
    {
        InitializeComponent();
        _nav = nav;

        // Wire the navigation service to the Frame inside this window
        if (_nav is NavigationService ns)
            ns.SetFrame(MainFrame);

        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        // Navigate to POS (Point of Sale) as the default landing page
        _nav.NavigateTo<PosViewModel>();

        // Initialize the POS page data
        if (MainFrame.Content is Views.Sales.PosView posView)
            await posView.InitAsync();
    }
}
