using DocumentManager.Application.Audit;
using DocumentManager.Application.Common;

namespace DocumentManager.Application.Interfaces;

public interface IAuditService
{
    Task RecordAsync(string action, string entityType, string? entityId, string description, object? additionalData = null, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<AuditLogDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
