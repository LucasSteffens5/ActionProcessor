using ActionProcessor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Data;

public class ActionProcessorDbContext : DbContext
{
    public ActionProcessorDbContext(DbContextOptions<ActionProcessorDbContext> options) : base(options)
    {
    }

    public DbSet<BatchUpload> BatchUploads => Set<BatchUpload>();
    public DbSet<ProcessingEvent> ProcessingEvents => Set<ProcessingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // BatchUpload configuration
        modelBuilder.Entity<BatchUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Events)
                .WithOne(e => e.Batch)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProcessingEvent configuration
        modelBuilder.Entity<ProcessingEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Document).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientIdentifier).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SideEffectsJson).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.ResponseData).HasMaxLength(4000);

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => new { e.Document, e.ClientIdentifier });
        });

        base.OnModelCreating(modelBuilder);
    }
}
