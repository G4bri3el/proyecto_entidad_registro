namespace DocumentManager.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? HttpMethod { get; }
    string? Endpoint { get; }
    int? StatusCode { get; }
    string CorrelationId { get; }
}
