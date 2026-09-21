using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WarehousePOS.Application.Reports;

namespace WarehousePOS.Desktop.Views.Reports;

public sealed class EmployeePickerOption
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public decimal BaseSalary { get; init; }
    public string DisplayName => $"{FullName} ({Role}) — Base: Rs. {BaseSalary:N2}";
}

public partial class ProcessEmployeePaymentDialog : Window
{
    public CreateEmployeePaymentRequest? Request { get; private set; }
    private readonly List<EmployeePickerOption> _employees;

    public ProcessEmployeePaymentDialog(
        IReadOnlyList<EmployeeReportDto> employees,
        EmployeeReportDto? preselectedEmployee = null)
    {
        InitializeComponent();

        _employees = employees.Select(e => new EmployeePickerOption
        {
            Id = e.EmployeeId,
            FullName = e.FullName,
            Role = e.Role,
            BaseSalary = e.BaseSalary
        }).ToList();

        CmbEmployees.ItemsSource = _employees;
        DpPaymentDate.SelectedDate = DateTime.Today;
        TxtPaymentTime.Text = DateTime.Now.ToString("HH:mm");

        if (preselectedEmployee != null)
        {
            var match = _employees.FirstOrDefault(e => e.Id == preselectedEmployee.EmployeeId);
            if (match != null)
                CmbEmployees.SelectedItem = match;
        }
        else if (_employees.Count > 0)
        {
            CmbEmployees.SelectedIndex = 0;
        }
    }

    private void CmbEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbEmployees.SelectedItem is EmployeePickerOption emp)
        {
            PnlEmployeeDetails.Visibility = Visibility.Visible;
            TxtEmpName.Text = emp.FullName;
            TxtEmpRole.Text = $"Role: {emp.Role}";
            TxtEmpBaseSalary.Text = $"Rs. {emp.BaseSalary:N2}";
            TxtAmount.Text = emp.BaseSalary.ToString("F2");
        }
        else
        {
            PnlEmployeeDetails.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (CmbEmployees.SelectedItem is not EmployeePickerOption selectedEmp)
        {
            MessageBox.Show("Please select an employee.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtAmount.Text, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid payment amount greater than zero.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string pTypeStr = CmbPaymentType.SelectedItem is ComboBoxItem item && item.Content.ToString()!.Contains("Advance")
            ? "Advance Payment"
            : "Total Sum";

        DateTime date = DpPaymentDate.SelectedDate ?? DateTime.Today;
        TimeSpan time = TimeSpan.Zero;
        if (TimeSpan.TryParse(TxtPaymentTime.Text, out var parsedTime))
        {
            time = parsedTime;
        }

        DateTime paymentDateTime = new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, 0, DateTimeKind.Local).ToUniversalTime();

        Request = new CreateEmployeePaymentRequest(
            selectedEmp.Id,
            amount,
            pTypeStr,
            TxtNotes.Text,
            paymentDateTime);

        DialogResult = true;
        Close();
    }
}
