using DocumentManager.Application.Common;
using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Users;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using DocumentManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Users;

public sealed class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuditService auditService,
    ApplicationDbContext dbContext) : IUserService
{
    public async Task<PagedResult<UserDto>> ListAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;
        var usersQuery = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            usersQuery = usersQuery.Where(user =>
                user.UserName!.Contains(query.Search) ||
                user.Email!.Contains(query.Search) ||
                user.FirstName.Contains(query.Search) ||
                user.LastName.Contains(query.Search));
        }

        var total = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery.OrderBy(user => user.UserName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToDtoAsync(user));
        }

        return PagedResult<UserDto>.Create(items, page, pageSize, total);
    }

    public async Task<UserDto> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRolesAsync(request.Roles);
        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        EnsureIdentitySuccess(result);

        if (request.Roles.Count > 0)
        {
            result = await userManager.AddToRolesAsync(user, request.Roles);
            EnsureIdentitySuccess(result);
        }

        await auditService.RecordAsync(AuditActions.UserCreated, nameof(ApplicationUser), user.Id, $"Usuario creado: {user.UserName}", new { request.Roles }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        user.Email = request.Email.Trim();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await auditService.RecordAsync(AuditActions.UserUpdated, nameof(ApplicationUser), user.Id, $"Usuario actualizado: {user.UserName}", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> SetStatusAsync(string id, SetUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!request.IsActive)
        {
            await userManager.UpdateSecurityStampAsync(user);
            var activeTokens = await dbContext.RefreshTokens.Where(token => token.UserId == id && token.RevokedAt == null).ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTimeOffset.UtcNow;
            }
        }

        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await auditService.RecordAsync(request.IsActive ? AuditActions.UserUpdated : AuditActions.UserDisabled, nameof(ApplicationUser), user.Id, $"Estado de usuario cambiado: {user.UserName}", new { request.IsActive }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(user);
    }

    public async Task AssignRoleAsync(string id, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRolesAsync([request.Role]);
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        if (!await userManager.IsInRoleAsync(user, request.Role))
        {
            EnsureIdentitySuccess(await userManager.AddToRoleAsync(user, request.Role));
        }

        await auditService.RecordAsync(AuditActions.RoleAssigned, nameof(ApplicationUser), user.Id, $"Rol asignado: {request.Role}", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleAsync(string id, string role, CancellationToken cancellationToken = default)
    {
        await ValidateRolesAsync([role]);
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        if (await userManager.IsInRoleAsync(user, role))
        {
            EnsureIdentitySuccess(await userManager.RemoveFromRoleAsync(user, role));
        }

        await auditService.RecordAsync(AuditActions.RoleRemoved, nameof(ApplicationUser), user.Id, $"Rol removido: {role}", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableTwoFactorForUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new NotFoundAppException("El usuario no existe.");
        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        await auditService.RecordAsync(AuditActions.TwoFactorDisabled, nameof(ApplicationUser), user.Id, $"2FA desactivado por administrador: {user.UserName}", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateRolesAsync(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (!AppRoles.All.Contains(role) || !await roleManager.RoleExistsAsync(role))
            {
                throw new ValidationAppException($"Rol invalido: {role}");
            }
        }
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.TwoFactorEnabled,
            user.CreatedAt,
            user.LastLoginAt,
            roles.ToArray());
    }

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new ValidationAppException(message);
    }
}
