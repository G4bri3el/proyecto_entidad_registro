using DocumentManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentManager.Infrastructure.Persistence.Configurations;

public sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("Folders");
        builder.HasKey(folder => folder.Id);
        builder.Property(folder => folder.Name).HasMaxLength(150).IsRequired();
        builder.Property(folder => folder.RowVersion).IsRowVersion();
        builder.HasOne(folder => folder.ParentFolder)
            .WithMany(folder => folder.Children)
            .HasForeignKey(folder => folder.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(folder => folder.ParentFolderId);
        builder.HasIndex(folder => folder.IsDeleted);
        builder.HasIndex(folder => new { folder.ParentFolderId, folder.Name })
            .HasFilter("\"IsDeleted\" = false AND \"ParentFolderId\" IS NOT NULL")
            .IsUnique();
        builder.HasIndex(folder => folder.Name)
            .HasDatabaseName("IX_Folders_Root_Name")
            .HasFilter("\"IsDeleted\" = false AND \"ParentFolderId\" IS NULL")
            .IsUnique();
    }
}