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
        modelBuilder.Entity<BatchUpload>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserEmail);
            entity.HasIndex(e => new { e.UserEmail, e.CreatedAt });

            entity.HasMany(e => e.Events)
                .WithOne(e => e.Batch)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<ProcessingEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Document).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientIdentifier).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SideEffectsJson).IsRequired();
            entity.Property(e => e.Status);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.ResponseData).HasMaxLength(4000);
            
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => new { e.Document, e.ClientIdentifier });
        });

        base.OnModelCreating(modelBuilder);
    }
}
