using DocumentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentManager.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.OriginalFileName).HasMaxLength(180).IsRequired();
        builder.Property(document => document.StoredFileName).HasMaxLength(120).IsRequired();
        builder.Property(document => document.StorageKey).HasMaxLength(260).IsRequired();
        builder.Property(document => document.MimeType).HasMaxLength(80).IsRequired();
        builder.Property(document => document.Extension).HasMaxLength(12).IsRequired();
        builder.Property(document => document.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(document => document.RowVersion).IsRowVersion();
        builder.HasOne(document => document.Folder)
            .WithMany(folder => folder.Documents)
            .HasForeignKey(document => document.FolderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(document => document.FolderId);
        builder.HasIndex(document => document.UploadedAt);
        builder.HasIndex(document => document.IsDeleted);
        builder.HasIndex(document => document.StorageKey).IsUnique();
    }
}
