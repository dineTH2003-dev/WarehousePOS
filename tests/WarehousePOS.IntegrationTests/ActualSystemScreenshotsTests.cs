using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using WarehousePOS.Application;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Desktop;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Auth;
using WarehousePOS.Desktop.ViewModels.Expenses;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Purchasing;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.ViewModels.Users;
using WarehousePOS.Desktop.Views.Auth;
using WarehousePOS.Desktop.Views.Expenses;
using WarehousePOS.Desktop.Views.Products;
using WarehousePOS.Desktop.Views.Purchasing;
using WarehousePOS.Desktop.Views.Reports;
using WarehousePOS.Desktop.Views.Sales;
using WarehousePOS.Desktop.Views.Settings;
using WarehousePOS.Desktop.Views.Suppliers;
using WarehousePOS.Desktop.Views.Users;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Infrastructure;
using Xunit;
using WpfApp = System.Windows.Application;
using WpfResourceDictionary = System.Windows.ResourceDictionary;

namespace WarehousePOS.IntegrationTests;

public class ActualSystemScreenshotsTests
{
    private static void RenderVisualToFile(Visual visual, int width, int height, string filePath)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fs = File.Create(filePath);
        encoder.Save(fs);
    }

    [Fact]
    public void GenerateActualScreenshots()
    {
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                var app = WpfApp.Current ?? new WpfApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                var appResources = app.Resources;
                if (appResources.MergedDictionaries.Count == 0)
                {
                    appResources.MergedDictionaries.Add(new WpfResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/WarehousePOS.Desktop;component/Resources/Styles/Colors.xaml", UriKind.Absolute)
                    });
                    appResources.MergedDictionaries.Add(new WpfResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/WarehousePOS.Desktop;component/Resources/Styles/Typography.xaml", UriKind.Absolute)
                    });
                    appResources.MergedDictionaries.Add(new WpfResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/WarehousePOS.Desktop;component/Resources/Styles/Controls.xaml", UriKind.Absolute)
                    });

                    appResources["HappyProductsLogo"] = new BitmapImage(
                        new Uri("pack://application:,,,/WarehousePOS.Desktop;component/Resources/Images/happy-products-logo.png", UriKind.Absolute)
                    );
                }

                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "WarehousePOS", "Data", "WarehousePOS.db");

                var services = new ServiceCollection();
                services.AddApplicationServices();
                services.AddInfrastructureServices(dbPath);
                services.AddSingleton<SessionContext>();
                services.AddSingleton<INavigationService, NavigationService>();

                services.AddScoped<LoginViewModel>();
                services.AddScoped<PosViewModel>();
                services.AddScoped<BillHistoryViewModel>();
                services.AddScoped<ProductListViewModel>();
                services.AddScoped<ProductFormViewModel>();
                services.AddScoped<PurchasingViewModel>();
                services.AddScoped<CategoryManagementViewModel>();
                services.AddScoped<SupplierListViewModel>();
                services.AddScoped<SupplierFormViewModel>();
                services.AddScoped<CustomerListViewModel>();
                services.AddScoped<CustomerFormViewModel>();
                services.AddScoped<CustomerPurchasedItemsViewModel>();
                services.AddScoped<ReportsViewModel>();
                services.AddScoped<ExpenseListViewModel>();
                services.AddScoped<UserManagementViewModel>();
                services.AddScoped<StoreSettingsViewModel>();

                services.AddTransient<LoginWindow>();
                services.AddScoped<PosView>();
                services.AddScoped<BillHistoryView>();
                services.AddScoped<ProductListView>();
                services.AddScoped<PurchasingView>();
                services.AddScoped<SupplierListView>();
                services.AddScoped<CustomerListView>();
                services.AddScoped<ReportsView>();
                services.AddScoped<ExpenseListView>();
                services.AddScoped<UserManagementView>();
                services.AddScoped<StoreSettingsView>();

                services.AddTransient<MainWindow>();

                NavigationService.Register<PosViewModel, PosView>();
                NavigationService.Register<BillHistoryViewModel, BillHistoryView>();
                NavigationService.Register<ProductListViewModel, ProductListView>();
                NavigationService.Register<PurchasingViewModel, PurchasingView>();
                NavigationService.Register<SupplierListViewModel, SupplierListView>();
                NavigationService.Register<CustomerListViewModel, CustomerListView>();
                NavigationService.Register<ReportsViewModel, ReportsView>();
                NavigationService.Register<ExpenseListViewModel, ExpenseListView>();
                NavigationService.Register<UserManagementViewModel, UserManagementView>();
                NavigationService.Register<StoreSettingsViewModel, StoreSettingsView>();

                var sp = services.BuildServiceProvider();

                var session = sp.GetRequiredService<SessionContext>();
                session.SetUser(new AuthResult(1, "HappyProducts", "Happy Products Admin", UserRole.Admin));

                var outDir = @"d:\WarehousePOS\docs\manual_assets";

                // 1. Render Actual Login Window
                var loginVm = sp.GetRequiredService<LoginViewModel>();
                loginVm.Username = "HappyProducts";
                var loginWindow = new LoginWindow(loginVm);
                loginWindow.UsernameBox.Text = "HappyProducts";
                loginWindow.PasswordBox.Password = "Indika@123";
                
                var loginContent = (FrameworkElement)loginWindow.Content;
                loginContent.Measure(new Size(1280, 720));
                loginContent.Arrange(new Rect(0, 0, 1280, 720));
                loginContent.UpdateLayout();
                RenderVisualToFile(loginContent, 1280, 720, Path.Combine(outDir, "actual_login.png"));

                // 2. Render Main Window with each view
                var mainWindow = sp.GetRequiredService<MainWindow>();
                mainWindow.InitializeShell();

                var mainContent = (FrameworkElement)mainWindow.Content;
                var setActiveNavMethod = typeof(MainWindow).GetMethod("SetActiveNav", BindingFlags.Instance | BindingFlags.NonPublic);

                void RenderPage<TView>(Button navBtn, string filename) where TView : FrameworkElement
                {
                    using var scope = sp.CreateScope();
                    var view = scope.ServiceProvider.GetRequiredService<TView>();

                    var initMethod = typeof(TView).GetMethod("InitAsync");
                    if (initMethod != null)
                    {
                        var task = (Task)initMethod.Invoke(view, null)!;
                        task.GetAwaiter().GetResult();
                    }

                    var frame = mainWindow.MainFrame;
                    var dispatcherFrame = new System.Windows.Threading.DispatcherFrame();
                    System.Windows.Navigation.NavigatedEventHandler? handler = null;
                    handler = (s, e) =>
                    {
                        frame.Navigated -= handler;
                        dispatcherFrame.Continue = false;
                    };
                    frame.Navigated += handler;
                    frame.Navigate(view);
                    System.Windows.Threading.Dispatcher.PushFrame(dispatcherFrame);

                    setActiveNavMethod?.Invoke(mainWindow, new object?[] { navBtn });

                    mainContent.Measure(new Size(1280, 768));
                    mainContent.Arrange(new Rect(0, 0, 1280, 768));
                    mainContent.UpdateLayout();

                    RenderVisualToFile(mainContent, 1280, 768, Path.Combine(outDir, filename));
                }

                RenderPage<PosView>(mainWindow.BtnPos, "actual_pos.png");
                RenderPage<BillHistoryView>(mainWindow.BtnBillHistory, "actual_bill_history.png");
                RenderPage<ProductListView>(mainWindow.BtnProducts, "actual_products.png");
                RenderPage<PurchasingView>(mainWindow.BtnPurchasing, "actual_purchasing.png");
                RenderPage<SupplierListView>(mainWindow.BtnSuppliers, "actual_suppliers.png");
                RenderPage<CustomerListView>(mainWindow.BtnCustomers, "actual_customers.png");
                RenderPage<ReportsView>(mainWindow.BtnReports, "actual_reports.png");
                RenderPage<ExpenseListView>(mainWindow.BtnExpenses, "actual_expenses.png");
                RenderPage<UserManagementView>(mainWindow.BtnUserManagement, "actual_users.png");
                RenderPage<StoreSettingsView>(mainWindow.BtnSettings, "actual_settings.png");
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
        {
            throw new Exception($"Failed to generate actual screenshots: {error}", error);
        }
    }
}
