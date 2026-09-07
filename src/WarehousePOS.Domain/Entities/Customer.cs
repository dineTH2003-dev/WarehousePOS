using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>A customer — retail walk-in or registered wholesale buyer.</summary>
public sealed class Customer : AggregateRoot
{
    private Customer() { }

    public string Name         { get; private set; } = string.Empty;
    public string? Phone       { get; private set; }
    public string? Email       { get; private set; }
    public string? Address     { get; private set; }
    public SaleType Type       { get; private set; } = SaleType.Retail;
    public decimal DiscountRate{ get; private set; } = 0m;
    public bool IsActive       { get; private set; } = true;

    public static Customer Create(
        string name,
        SaleType type        = SaleType.Retail,
        string? phone        = null,
        string? email        = null,
        string? address      = null,
        decimal discountRate = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateContactInformation(phone, email);
        ValidateDiscountRate(discountRate);
        return new Customer
        {
            Name         = name.Trim(),
            Type         = type,
            Phone        = phone?.Trim(),
            Email        = email?.Trim(),
            Address      = address?.Trim(),
            DiscountRate = discountRate
        };
    }

    public void Update(string name, SaleType type, string? phone, string? email, string? address, decimal discountRate = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateContactInformation(phone, email);
        ValidateDiscountRate(discountRate);
        Name         = name.Trim();
        Type         = type;
        Phone        = phone?.Trim();
        Email        = email?.Trim();
        Address      = address?.Trim();
        DiscountRate = discountRate;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }

    private static void ValidateDiscountRate(decimal discountRate)
    {
        if (discountRate is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "Discount rate must be between 0 and 100.");
    }

    private static void ValidateContactInformation(string? phone, string? email)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var trimmedPhone = phone.Trim();
            if (trimmedPhone.Length != 10 || !trimmedPhone.StartsWith('0') || trimmedPhone.Any(character => character is < '0' or > '9'))
                throw new ArgumentException("Phone number must consist of 10 digits starting with '0'.", nameof(phone));
        }

        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            throw new ArgumentException("Please enter a valid email address.", nameof(email));
    }

    private static bool IsValidEmail(string email)
    {
        var trimmed = email.Trim();
        if (!new EmailAddressAttribute().IsValid(trimmed))
            return false;

        try
        {
            var address = new MailAddress(trimmed);
            var domain = address.Host;
            return address.Address == trimmed && domain.Contains('.') &&
                   !domain.StartsWith('.') && !domain.EndsWith('.') &&
                   !domain.Contains("..", StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
