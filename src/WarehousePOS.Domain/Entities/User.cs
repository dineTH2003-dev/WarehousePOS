using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// A user who can log in to the system.
/// Passwords are stored as BCrypt hashes — NEVER plaintext.
/// </summary>
public sealed class User : AggregateRoot
{
    private User() { }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastLoginAt { get; private set; }
    public decimal BaseSalary { get; private set; } = 0m;
    public decimal CommissionRate { get; private set; } = 0m;
    public string? SalaryComponents { get; private set; }
    public string? PaymentDueDate { get; private set; }
    public string? PaymentFrequency { get; private set; } = "Monthly";

    public ICollection<DeliveryTrip> DeliveryTrips { get; private set; } = new List<DeliveryTrip>();
    public ICollection<FuelLog> FuelLogs { get; private set; } = new List<FuelLog>();
    public ICollection<ServiceTicket> ServiceTickets { get; private set; } = new List<ServiceTicket>();


    public static User Create(string username, string passwordHash, string fullName, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role
        };
    }

    public void UpdateSalaryDetails(decimal baseSalary, string? salaryComponents, string? paymentDueDate, string? paymentFrequency = "Monthly", decimal commissionRate = 0m)
    {
        BaseSalary = baseSalary < 0 ? 0m : baseSalary;
        SalaryComponents = salaryComponents?.Trim();
        PaymentDueDate = paymentDueDate?.Trim();
        PaymentFrequency = string.IsNullOrWhiteSpace(paymentFrequency) ? "Monthly" : paymentFrequency.Trim();
        CommissionRate = commissionRate < 0 ? 0m : commissionRate;
        SetUpdatedAt();
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void ChangePasswordHash(string newHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        PasswordHash = newHash;
        SetUpdatedAt();
    }

    public void UpdateProfile(string fullName, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        FullName = fullName.Trim();
        Role = role;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
