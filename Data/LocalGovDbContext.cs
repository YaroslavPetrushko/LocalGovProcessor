using LocalGovProcessor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalGovProcessor.Data;

public class LocalGovDbContext : DbContext
{
    public LocalGovDbContext(DbContextOptions<LocalGovDbContext> options)
        : base(options)
    {
    }

    public DbSet<CommunityEntity> Communities => Set<CommunityEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<SectionEntity> Sections => Set<SectionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommunityEntity>(entity =>
        {
            entity.ToTable("communities");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(x => x.Region).HasColumnName("region").HasMaxLength(255);
            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            entity.HasIndex(x => x.Region).HasDatabaseName("idx_communities_region");
            entity.HasAlternateKey(x => new { x.Name, x.Region }).HasName("uq_community");
        });

        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();
            entity.Property(x => x.CommunityId).HasColumnName("community_id");
            entity.Property(x => x.Year).HasColumnName("year");
            entity.Property(x => x.DocType).HasColumnName("doc_type").HasMaxLength(64);
            entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255);
            entity.Property(x => x.FileFormat).HasColumnName("file_format").HasMaxLength(8);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            entity.Property(x => x.RawText).HasColumnName("raw_text");
            entity.Property(x => x.ProcessingTimeMs).HasColumnName("processing_time_ms");
            entity.Property(x => x.UploadedAt)
                .HasColumnName("uploaded_at")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            entity.HasIndex(x => x.CommunityId).HasDatabaseName("idx_documents_community");
            entity.HasIndex(x => x.Year).HasDatabaseName("idx_documents_year");
            entity.HasIndex(x => x.Status).HasDatabaseName("idx_documents_status");
            entity.HasAlternateKey(x => new { x.CommunityId, x.Year, x.DocType, x.FileName })
                .HasName("uq_document");

            entity.HasOne(x => x.Community)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SectionEntity>(entity =>
        {
            entity.ToTable("sections");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();
            entity.Property(x => x.DocumentId).HasColumnName("document_id");
            entity.Property(x => x.Position).HasColumnName("position");
            entity.Property(x => x.Level).HasColumnName("level");
            entity.Property(x => x.Title).HasColumnName("title");
            entity.Property(x => x.Content).HasColumnName("content");

            entity.HasAlternateKey(x => new { x.DocumentId, x.Position })
                .HasName("uq_section");
            entity.HasIndex(x => new { x.DocumentId, x.Position })
                .HasDatabaseName("idx_sections_document");

            entity.HasOne(x => x.Document)
                .WithMany(x => x.Sections)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
