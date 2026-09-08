using DocumentManager.Application.Common;

namespace DocumentManager.Application.Users;

public sealed record UserDto(
    string Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    bool TwoFactorEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyCollection<string> Roles);

public sealed record UserQuery(string? Search, int Page = 1, int PageSize = 25) : PageRequest(Page, PageSize);

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    IReadOnlyCollection<string> Roles);

public sealed record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName);

public sealed record SetUserStatusRequest(bool IsActive);

public sealed record AssignRoleRequest(string Role);
