using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace WarehousePOS.Desktop.Validation;

public static class ContactValidation
{
    public static string? GetPhoneError(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;
        if (phone.Length > 10)
            return "Phone number cannot exceed 10 digits.";
        return phone.Any(character => character is < '0' or > '9')
            ? "Phone number must contain digits only."
            : null;
    }

    public static string? GetEmailError(string email) =>
        string.IsNullOrWhiteSpace(email) || IsValidEmail(email)
            ? null
            : "Please enter a valid email address.";

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
