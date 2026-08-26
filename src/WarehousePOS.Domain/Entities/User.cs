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

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
