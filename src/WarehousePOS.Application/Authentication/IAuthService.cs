namespace WarehousePOS.Application.Authentication;

/// <summary>
/// Authentication service — handles login and user validation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to log in with the given credentials.
    /// Returns <see cref="AuthResult"/> on success, or null on failure.
    /// </summary>
    Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Verifies that a plain password matches the stored hash for a given user.</summary>
    Task<bool> VerifyPasswordAsync(int userId, string plainPassword, CancellationToken ct = default);
}
