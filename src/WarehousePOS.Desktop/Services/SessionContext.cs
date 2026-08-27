using WarehousePOS.Application.Authentication;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.Services;

/// <summary>
/// Holds the currently logged-in user for the lifetime of the session.
/// Singleton — injected wherever role checks are needed.
/// </summary>
public sealed class SessionContext
{
    private AuthResult? _currentUser;

    public AuthResult CurrentUser =>
        _currentUser ?? throw new InvalidOperationException("No user is currently logged in.");

    public bool IsLoggedIn => _currentUser is not null;

    public bool IsAdmin => _currentUser?.Role == UserRole.Admin;

    public void SetUser(AuthResult user)
    {
        _currentUser = user ?? throw new ArgumentNullException(nameof(user));
    }

    public void Clear()
    {
        _currentUser = null;
    }
}
