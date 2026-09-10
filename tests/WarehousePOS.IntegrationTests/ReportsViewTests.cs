using System;
using System.Threading;
using Moq;
using WarehousePOS.Application.Reports;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.Views.Reports;
using Xunit;
using WpfApp = System.Windows.Application;
using WpfResourceDictionary = System.Windows.ResourceDictionary;

namespace WarehousePOS.IntegrationTests;

public class ReportsViewTests
{
    [Fact]
    public void ReportsView_InitializeComponent_ShouldNotThrowXamlParseException()
    {
        Exception? exception = null;

        var thread = new Thread(() =>
        {
            try
            {
                // Ensure WPF Application instance exists with merged dictionaries for StaticResource resolution
                var app = WpfApp.Current ?? new WpfApp { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

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
                }

                var mockReportService = new Mock<IReportService>();
                var mockSupplierService = new Mock<ISupplierService>();
                var sessionContext = new SessionContext();
                sessionContext.SetUser(new WarehousePOS.Application.Authentication.AuthResult(1, "admin", "Test Admin", WarehousePOS.Domain.Enums.UserRole.Admin));

                var vm = new ReportsViewModel(mockReportService.Object, mockSupplierService.Object, sessionContext);
                var reportsView = new ReportsView(vm);

                Assert.NotNull(reportsView);
                Assert.Same(vm, reportsView.DataContext);

                // Several WPF type converters run only when a view is measured
                // and arranged. Exercise that path as well as XAML construction.
                reportsView.Measure(new System.Windows.Size(1280, 800));
                reportsView.Arrange(new System.Windows.Rect(0, 0, 1280, 800));
                reportsView.UpdateLayout();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw exception;
        }
    }
}
