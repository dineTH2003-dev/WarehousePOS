using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using WarehousePOS.Application.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ImportProductsDialog : Window
{
    private readonly IProductImportService _importService;
    private readonly int _userId;
    private string? _selectedFilePath;

    public bool HasImportedAny { get; private set; }

    public ImportProductsDialog(IProductImportService importService, int userId = 1)
    {
        InitializeComponent();
        _importService = importService;
        _userId = userId;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Product CSV File",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _selectedFilePath = openFileDialog.FileName;
            TxtFilePath.Text = _selectedFilePath;
            ResultBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "Save Product Import Template",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = "Product_Import_Template.csv"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                var templateBytes = _importService.GenerateSampleTemplateCsv();
                File.WriteAllBytes(saveFileDialog.FileName, templateBytes);
                MessageBox.Show($"Template successfully saved to:\n{saveFileDialog.FileName}",
                    "Template Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save template: {ex.Message}",
                    "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            MessageBox.Show("Please choose a valid CSV file first.", "No File Selected",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LoadingBorder.Visibility = Visibility.Visible;
        BtnImport.IsEnabled = false;
        ResultBorder.Visibility = Visibility.Collapsed;

        try
        {
            await using var stream = File.OpenRead(_selectedFilePath);
            var options = new ProductImportOptions(
                UpdateExisting: ChkUpdateExisting.IsChecked == true,
                DefaultCategory: "General");

            var result = await _importService.ImportCsvAsync(stream, options, _userId);

            if (result.SuccessCount > 0)
            {
                HasImportedAny = true;
            }

            DisplayResult(result);
        }
        catch (Exception ex)
        {
            ResultBorder.Visibility = Visibility.Visible;
            ResultBorder.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
            ResultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TxtResultHeading.Text = "Import Failed";
            TxtResultHeading.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            TxtResultDetails.Text = $"An unexpected error occurred during import: {ex.Message}";
            TxtErrorsHeading.Visibility = Visibility.Collapsed;
            LstErrors.Visibility = Visibility.Collapsed;
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
            BtnImport.IsEnabled = true;
            BtnClose.Visibility = Visibility.Visible;
        }
    }

    private void DisplayResult(ProductImportResultDto result)
    {
        ResultBorder.Visibility = Visibility.Visible;

        if (result.ErrorCount == 0)
        {
            // Complete success
            ResultBorder.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
            ResultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            TxtResultHeading.Text = "✓ Import Completed Successfully";
            TxtResultHeading.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
            TxtResultDetails.Text = result.SummaryMessage;
            TxtErrorsHeading.Visibility = Visibility.Collapsed;
            LstErrors.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Partial success or errors
            ResultBorder.Background = new SolidColorBrush(Color.FromRgb(255, 251, 235));
            ResultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            TxtResultHeading.Text = result.SuccessCount > 0 ? "⚠ Import Completed with Warnings" : "❌ Import Failed";
            TxtResultHeading.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6));
            TxtResultDetails.Text = result.SummaryMessage;

            if (result.Errors.Count > 0)
            {
                TxtErrorsHeading.Visibility = Visibility.Visible;
                LstErrors.Visibility = Visibility.Visible;
                LstErrors.ItemsSource = result.Errors.Select(err =>
                    $"Line {err.RowNumber}: {(string.IsNullOrWhiteSpace(err.Sku) ? string.Empty : $"[{err.Sku}] ")}{err.ProductName} — {err.ErrorMessage}").ToList();
            }
            else
            {
                TxtErrorsHeading.Visibility = Visibility.Collapsed;
                LstErrors.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = HasImportedAny;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = HasImportedAny;
        Close();
    }
}
