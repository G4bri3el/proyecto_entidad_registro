using DocumentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentManager.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Action).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.EntityId).HasMaxLength(120);
        builder.Property(audit => audit.Description).HasMaxLength(500).IsRequired();
        builder.Property(audit => audit.IpAddress).HasMaxLength(80);
        builder.Property(audit => audit.UserAgent).HasMaxLength(500);
        builder.Property(audit => audit.HttpMethod).HasMaxLength(16);
        builder.Property(audit => audit.Endpoint).HasMaxLength(300);
        builder.Property(audit => audit.CorrelationId).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.AdditionalData).HasColumnType("nvarchar(max)");
        builder.HasIndex(audit => audit.UserId);
        builder.HasIndex(audit => audit.Action);
        builder.HasIndex(audit => audit.CreatedAt);
        builder.HasIndex(audit => new { audit.EntityType, audit.EntityId });
    }
}
