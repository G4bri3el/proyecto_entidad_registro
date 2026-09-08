namespace DocumentManager.Application.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string? UserId,
    string? Username,
    string Action,
    string EntityType,
    string? EntityId,
    string Description,
    string? IpAddress,
    string? UserAgent,
    string? HttpMethod,
    string? Endpoint,
    int? StatusCode,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string? AdditionalData);
