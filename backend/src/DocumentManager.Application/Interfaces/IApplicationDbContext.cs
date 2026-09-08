using DocumentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Folder> Folders { get; }
    DbSet<Document> Documents { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
