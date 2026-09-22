using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Application.Authentication;

public sealed record UserDto(
    int Id,
    string Username,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime? LastLoginAt,
    decimal BaseSalary = 0m,
    string? SalaryComponents = null,
    string? PaymentDueDate = null,
    string? PaymentFrequency = "Monthly");

public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string Password,
    UserRole Role,
    decimal BaseSalary = 0m,
    string? SalaryComponents = null,
    string? PaymentDueDate = null,
    string? PaymentFrequency = "Monthly");

public sealed record UpdateUserRequest(
    int UserId,
    string FullName,
    UserRole Role,
    string? NewPassword = null,
    decimal BaseSalary = 0m,
    string? SalaryComponents = null,
    string? PaymentDueDate = null,
    string? PaymentFrequency = "Monthly");