using DocumentManager.Application.Common;

namespace DocumentManager.Application.Audit;

public sealed record AuditLogQuery(
    string? UserId,
    string? Action,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? EntityType,
    string? EntityId,
    int Page = 1,
    int PageSize = 25) : PageRequest(Page, PageSize);
