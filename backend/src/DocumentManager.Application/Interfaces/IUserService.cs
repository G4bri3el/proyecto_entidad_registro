using DocumentManager.Application.Common;
using DocumentManager.Application.Users;

namespace DocumentManager.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserDto> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> SetStatusAsync(string id, SetUserStatusRequest request, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string id, AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string id, string role, CancellationToken cancellationToken = default);
    Task DisableTwoFactorForUserAsync(string id, CancellationToken cancellationToken = default);
}
