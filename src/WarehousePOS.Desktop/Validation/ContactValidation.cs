using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace WarehousePOS.Desktop.Validation;

public static class ContactValidation
{
    public static string? GetPhoneError(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        if (!trimmed.StartsWith('0'))
            return "Phone number must start with '0' (e.g. 0717454667).";

        if (trimmed.Any(character => character is < '0' or > '9'))
            return "Phone number must contain digits only.";

        if (trimmed.Length != 10)
            return "Phone number must consist of 10 digits (e.g. 0717454667).";

        return null;
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
