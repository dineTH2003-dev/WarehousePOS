namespace WarehousePOS.Application.Common;

/// <summary>
/// Abstraction for password hashing and verification.
/// Implementation lives in Infrastructure (BCrypt).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
