using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Infrastructure.Data;
using ActionProcessor.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Repositories;

public class BatchRepository(ActionProcessorDbContext context) : IBatchRepository
{
    public async Task<BatchUpload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.BatchUploads
            .Include(b => b.Events)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<BatchUpload> AddAsync(BatchUpload batch, CancellationToken cancellationToken = default)
    {
        context.BatchUploads.Add(batch);
        await context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task UpdateAsync(BatchUpload batch, CancellationToken cancellationToken = default)
    {
        context.BatchUploads.Update(batch);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<BatchUpload>> GetAllAsync(int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await context.BatchUploads
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BatchUpload>> GetByEmailAsync(string userEmail, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
        => await context.BatchUploads
            .Where(b => b.UserEmail == userEmail)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);


    public async Task<BatchUpload?> GetActiveBatchByEmailAsync(string userEmail,
        CancellationToken cancellationToken = default)
        => await context.BatchUploads
            .Include(b => b.Events)
            .Where(b => b.UserEmail == userEmail &&
                        (b.Status == BatchStatus.Uploaded || b.Status == BatchStatus.Processing))
            .FirstOrDefaultAsync(cancellationToken);


    public async Task<bool> HasPendingEventsByEmailAsync(string userEmail,
        CancellationToken cancellationToken = default)
        => await context.BatchUploads
            .Include(b => b.Events)
            .Where(b => b.UserEmail == userEmail)
            .AnyAsync(b => b.Events.Any(e => e.Status == EventStatus.Pending || e.Status == EventStatus.Processing),
                cancellationToken);


    public async Task<IEnumerable<BatchUpload>> GetBatchesByEmailOrderedAsync(string userEmail, int skip = 0,
        int take = 100, CancellationToken cancellationToken = default)
        => await context.BatchUploads
            .Where(b => b.UserEmail == userEmail)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);


    public async Task<bool> TryUpdateAsync(BatchUpload batch, CancellationToken cancellationToken = default)
        => await EfRetryHelper.RetryOnConcurrencyAsync(
            async () =>
            {
                context.BatchUploads.Update(batch);
                await context.SaveChangesAsync(cancellationToken);
            },
            async () => await context.Entry(batch).ReloadAsync(cancellationToken)
        );


    public async Task<bool> StartProcessingAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await GetByIdAsync(batchId, cancellationToken);
        if (batch == null) return false;

        if (batch.StartProcessing())
        {
            return await TryUpdateAsync(batch, cancellationToken);
        }

        return false;
    }
}