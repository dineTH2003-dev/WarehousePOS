using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Authentication;

public sealed class UserManagementService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IUserManagementService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(int actorUserId, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserId, ct);
        return (await userRepository.GetAllAsync(ct)).Select(Map).ToList();
    }

    public async Task<UserDto> CreateAsync(int actorUserId, CreateUserRequest request, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserId, ct);

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Username and full name are required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");
        if (await userRepository.GetByUsernameAsync(request.Username.Trim().ToLowerInvariant(), ct) is not null)
            throw new InvalidOperationException("That username is already in use.");

        var user = User.Create(
            request.Username,
            passwordHasher.Hash(request.Password),
            request.FullName,
            request.Role);
        await userRepository.AddAsync(user, ct);
        return Map(user);
    }

    public async Task<UserDto> UpdateAsync(int actorUserId, UpdateUserRequest request, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserId, ct);
        var user = await GetRequiredAsync(request.UserId, ct);
        var wasAdmin = user.Role == UserRole.Admin;

        if (actorUserId == request.UserId && request.Role != UserRole.Admin)
            throw new InvalidOperationException("You cannot remove Admin access from the currently logged-in account.");

        if (wasAdmin && request.Role != UserRole.Admin && await CountActiveAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("At least one active Admin account must remain.");

        user.UpdateProfile(request.FullName, request.Role);
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));

        await userRepository.UpdateAsync(user, ct);
        return Map(user);
    }

    public async Task DeactivateAsync(int actorUserId, int userId, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserId, ct);
        if (actorUserId == userId)
            throw new InvalidOperationException("You cannot deactivate the currently logged-in account.");

        var user = await GetRequiredAsync(userId, ct);
        if (user.Role == UserRole.Admin && user.IsActive && await CountActiveAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("At least one active Admin account must remain.");

        user.Deactivate();
        await userRepository.UpdateAsync(user, ct);
    }

    public async Task ActivateAsync(int actorUserId, int userId, CancellationToken ct = default)
    {
        await RequireAdminAsync(actorUserId, ct);
        var user = await GetRequiredAsync(userId, ct);
        user.Activate();
        await userRepository.UpdateAsync(user, ct);
    }

    private async Task<User> RequireAdminAsync(int actorUserId, CancellationToken ct)
    {
        var actor = await GetRequiredAsync(actorUserId, ct);
        if (!actor.IsActive || actor.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Only an Admin can manage users.");
        return actor;
    }

    private async Task<User> GetRequiredAsync(int userId, CancellationToken ct) =>
        await userRepository.GetByIdAsync(userId, ct)
        ?? throw new InvalidOperationException("User account was not found.");

    private async Task<int> CountActiveAdminsAsync(CancellationToken ct) =>
        (await userRepository.GetAllAsync(ct)).Count(u => u.IsActive && u.Role == UserRole.Admin);

    private static UserDto Map(User user) =>
        new(user.Id, user.Username, user.FullName, user.Role, user.IsActive, user.LastLoginAt);
}