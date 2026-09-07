using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Authentication;

public sealed record UserDto(
    int Id,
    string Username,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime? LastLoginAt);

public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string Password,
    UserRole Role);

public sealed record UpdateUserRequest(
    int UserId,
    string FullName,
    UserRole Role,
    string? NewPassword = null);