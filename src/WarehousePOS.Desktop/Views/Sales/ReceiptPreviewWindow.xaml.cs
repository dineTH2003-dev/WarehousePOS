using System.Windows;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Sales;
using WarehousePOS.Infrastructure.Printing;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class ReceiptPreviewWindow : Window
{
    private readonly SaleDto _sale;
    private readonly IReceiptPrinter _printer;

    public ReceiptPreviewWindow(
        SaleDto sale,
        IReceiptPrinter printer,
        string storeName = "WAREHOUSE POS & WHOLESALE",
        string storeAddress = "123 Main Street, Colombo, Sri Lanka",
        string storePhone = "Tel: 011-2345678",
        string footerMessage = "Thank you for your business!")
    {
        InitializeComponent();
        _sale = sale;
        _printer = printer;

        TxtReceiptText.Text = EpsonLq310Printer.FormatReceiptText(sale, storeName, storeAddress, storePhone, footerMessage);
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _printer.PrintReceiptAsync(_sale);
            MessageBox.Show("Print job sent to Epson LQ-310 printer successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to print receipt: {ex.Message}", "Printing Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
