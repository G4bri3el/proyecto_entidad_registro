using System.Text.Json;
using DocumentManager.Application.Audit;
using DocumentManager.Application.Common;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Audit;

public sealed class AuditService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IAuditService
{
    public Task RecordAsync(string action, string entityType, string? entityId, string description, object? additionalData = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId,
            Username = currentUser.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent,
            HttpMethod = currentUser.HttpMethod,
            Endpoint = currentUser.Endpoint,
            StatusCode = currentUser.StatusCode,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = currentUser.CorrelationId,
            AdditionalData = additionalData is null ? null : JsonSerializer.Serialize(additionalData)
        };

        dbContext.AuditLogs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.SafePage;
        var pageSize = query.SafePageSize;

        var auditQuery = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            auditQuery = auditQuery.Where(log => log.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditQuery = auditQuery.Where(log => log.Action == query.Action);
        }

        if (query.From is not null)
        {
            auditQuery = auditQuery.Where(log => log.CreatedAt >= query.From);
        }

        if (query.To is not null)
        {
            auditQuery = auditQuery.Where(log => log.CreatedAt <= query.To);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            auditQuery = auditQuery.Where(log => log.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            auditQuery = auditQuery.Where(log => log.EntityId == query.EntityId);
        }

        var total = await auditQuery.CountAsync(cancellationToken);
        var items = await auditQuery
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => ToDto(log))
            .ToListAsync(cancellationToken);

        return PagedResult<AuditLogDto>.Create(items, page, pageSize, total);
    }

    public async Task<AuditLogDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await dbContext.AuditLogs.AsNoTracking().FirstOrDefaultAsync(audit => audit.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("El registro de auditoria no existe.");

        return ToDto(log);
    }

    private static AuditLogDto ToDto(AuditLog log) =>
        new(
            log.Id,
            log.UserId,
            log.Username,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Description,
            log.IpAddress,
            log.UserAgent,
            log.HttpMethod,
            log.Endpoint,
            log.StatusCode,
            log.CreatedAt,
            log.CorrelationId,
            log.AdditionalData);
}
