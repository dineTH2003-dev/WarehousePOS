using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Authentication;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var user = await userRepository.GetByUsernameAsync(request.Username.Trim().ToLowerInvariant(), ct);

        if (user is null)
        {
            logger.LogWarning("Login attempt for unknown username: {Username}", request.Username);
            return null;
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login attempt for inactive user: {Username}", request.Username);
            return null;
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return null;
        }

        user.RecordLogin();
        await userRepository.UpdateAsync(user, ct);

        logger.LogInformation("User {Username} logged in successfully", user.Username);
        return new AuthResult(user.Id, user.Username, user.FullName, user.Role);
    }

    public async Task<bool> VerifyPasswordAsync(int userId, string plainPassword, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive) return false;
        return passwordHasher.Verify(plainPassword, user.PasswordHash);
    }
}
