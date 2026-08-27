using BC = BCrypt.Net.BCrypt;
using WarehousePOS.Application.Common;

namespace WarehousePOS.Infrastructure.Security;

/// <summary>BCrypt-based password hasher. Work factor 12 is production-safe.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword) =>
        BC.HashPassword(plainPassword, workFactor: WorkFactor);

    public bool Verify(string plainPassword, string hash) =>
        BC.Verify(plainPassword, hash);
}
