using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Authentication;

/// <summary>Login request DTO.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>Result returned after a successful login.</summary>
public sealed record AuthResult(
    int UserId,
    string Username,
    string FullName,
    UserRole Role);
