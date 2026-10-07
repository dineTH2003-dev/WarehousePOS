using System.Windows;
using WarehousePOS.Application.Reports;

namespace WarehousePOS.Desktop.Views.Reports;

public sealed class EmployeePaymentDisplayItem
{
    public string FormattedDate { get; init; } = string.Empty;
    public string PaymentType { get; init; } = string.Empty;
    public bool IsAdvance { get; init; }
    public string FormattedAmount { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string RecordedByName { get; init; } = string.Empty;
}

public partial class EmployeeWageHistoryDialog : Window
{
    public EmployeeWageHistoryDialog(EmployeeReportDto employee)
    {
        InitializeComponent();

        TxtEmployeeName.Text = employee.FullName;
        TxtEmployeeRole.Text = employee.Role;
        TxtEmployeeSubtitle.Text = $"Username: @{employee.Username} | Employee ID: #{employee.EmployeeId}";

        TxtBaseSalary.Text = $"Rs. {employee.BaseSalary:N2}";
        TxtTotalPaid.Text = $"Rs. {employee.TotalSalaryPaid:N2}";
        TxtTotalAdvances.Text = $"Rs. {employee.TotalAdvancesPaid:N2}";
        TxtBalanceDue.Text = $"Rs. {employee.NetBalanceDue:N2}";

        var displayItems = employee.PaymentHistory
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new EmployeePaymentDisplayItem
            {
                FormattedDate = p.PaymentDate.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                PaymentType = p.PaymentType,
                IsAdvance = p.PaymentType.Contains("Advance", StringComparison.OrdinalIgnoreCase),
                FormattedAmount = $"Rs. {p.Amount:N2}",
                Description = string.IsNullOrWhiteSpace(p.Description) ? "—" : p.Description,
                RecordedByName = string.IsNullOrWhiteSpace(p.RecordedByUserName) ? "Admin" : p.RecordedByUserName
            })
            .ToList();

        if (displayItems.Count == 0)
        {
            DgPayments.Visibility = Visibility.Collapsed;
            TxtEmptyNotice.Visibility = Visibility.Visible;
        }
        else
        {
            DgPayments.ItemsSource = displayItems;
            DgPayments.Visibility = Visibility.Visible;
            TxtEmptyNotice.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
