using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>A customer — retail walk-in or registered wholesale buyer.</summary>
public sealed class Customer : AggregateRoot
{
    private Customer() { }

    public string Name      { get; private set; } = string.Empty;
    public string? Phone    { get; private set; }
    public string? Email    { get; private set; }
    public string? Address  { get; private set; }
    public SaleType Type    { get; private set; } = SaleType.Retail;
    public bool IsActive    { get; private set; } = true;

    public static Customer Create(
        string name,
        SaleType type   = SaleType.Retail,
        string? phone   = null,
        string? email   = null,
        string? address = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateContactInformation(phone, email);
        return new Customer
        {
            Name    = name.Trim(),
            Type    = type,
            Phone   = phone?.Trim(),
            Email   = email?.Trim(),
            Address = address?.Trim()
        };
    }

    public void Update(string name, SaleType type, string? phone, string? email, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateContactInformation(phone, email);
        Name    = name.Trim();
        Type    = type;
        Phone   = phone?.Trim();
        Email   = email?.Trim();
        Address = address?.Trim();
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }

    private static void ValidateContactInformation(string? phone, string? email)
    {
        if (!string.IsNullOrWhiteSpace(phone) &&
            (phone.Length > 10 || phone.Any(character => character is < '0' or > '9')))
            throw new ArgumentException("Phone number must contain digits only and cannot exceed 10 digits.", nameof(phone));

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
