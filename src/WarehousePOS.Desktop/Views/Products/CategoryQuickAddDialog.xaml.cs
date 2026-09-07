using System.Windows;
using WarehousePOS.Application.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class CategoryQuickAddDialog : Window
{
    private readonly ICategoryService _categoryService;

    public CategoryDto? CreatedCategory { get; private set; }
    public bool NavigateToFullPageRequested { get; private set; }

    public CategoryQuickAddDialog(ICategoryService categoryService)
    {
        InitializeComponent();
        _categoryService = categoryService;
        Loaded += (_, _) => TxtName.Focus();
    }

    private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            TxtNameInlineError.Text = "Category name is required.";
            TxtNameInlineError.Visibility = Visibility.Visible;
        }
        else
        {
            TxtNameInlineError.Text = string.Empty;
            TxtNameInlineError.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        var desc = TxtDescription.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Category name is required.");
            return;
        }

        try
        {
            CreatedCategory = await _categoryService.CreateAsync(new CreateCategoryRequest(name, string.IsNullOrWhiteSpace(desc) ? null : desc));
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnOpenFullPage_Click(object sender, RoutedEventArgs e)
    {
        NavigateToFullPageRequested = true;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}
