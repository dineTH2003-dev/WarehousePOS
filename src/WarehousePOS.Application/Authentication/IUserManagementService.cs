namespace WarehousePOS.Application.Authentication;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(int actorUserId, CancellationToken ct = default);
    Task<UserDto> CreateAsync(int actorUserId, CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int actorUserId, UpdateUserRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int actorUserId, int userId, CancellationToken ct = default);
    Task ActivateAsync(int actorUserId, int userId, CancellationToken ct = default);
}