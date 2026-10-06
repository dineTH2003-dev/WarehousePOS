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
        string storeName = "HAPPY PRODUCTS",
        string storeAddress = "Bandaragama Rd, Waskaduwa",
        string storePhone = "Tel: 0711435343",
        string footerMessage = "During the warranty period, all goods must be delivered to the manufacturing facility for repairs. The company warranty or corporate bill must be presented. Items cannot be returned after sale; items should be fully inspected and accepted upon receipt.",
        string taxRegNo = "Damro, Abans, Singer, Soft Logic, Arpico Authorised Dealer")
    {
        InitializeComponent();
        _sale = sale;
        _printer = printer;

        TxtReceiptText.Text = EpsonLq310Printer.FormatReceiptText(sale, storeName, storeAddress, storePhone, footerMessage, taxRegNo);
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
