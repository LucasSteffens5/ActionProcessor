using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Repositories;

public class BatchRepository : IBatchRepository
{
    private readonly ActionProcessorDbContext _context;

    public BatchRepository(ActionProcessorDbContext context)
    {
        _context = context;
    }

    public async Task<BatchUpload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BatchUploads
            .Include(b => b.Events)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<BatchUpload> AddAsync(BatchUpload batch, CancellationToken cancellationToken = default)
    {
        _context.BatchUploads.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task UpdateAsync(BatchUpload batch, CancellationToken cancellationToken = default)
    {
        _context.BatchUploads.Update(batch);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<BatchUpload>> GetAllAsync(int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.BatchUploads
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BatchUpload>> GetByEmailAsync(string userEmail, int skip = 0, int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.BatchUploads
            .Where(b => b.UserEmail == userEmail)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<BatchUpload?> GetActiveBatchByEmailAsync(string userEmail,
        CancellationToken cancellationToken = default)
        => await _context.BatchUploads
            .Include(b => b.Events)
            .Where(b => b.UserEmail == userEmail &&
                        (b.Status == BatchStatus.Uploaded || b.Status == BatchStatus.Processing))
            .FirstOrDefaultAsync(cancellationToken);


    public async Task<bool> HasPendingEventsByEmailAsync(string userEmail,
        CancellationToken cancellationToken = default)
    {
        return await _context.BatchUploads
            .Include(b => b.Events)
            .Where(b => b.UserEmail == userEmail)
            .AnyAsync(b => b.Events.Any(e => e.Status == EventStatus.Pending || e.Status == EventStatus.Processing),
                cancellationToken);
    }

    public async Task<IEnumerable<BatchUpload>> GetBatchesByEmailOrderedAsync(string userEmail, int skip = 0,
        int take = 100, CancellationToken cancellationToken = default)
    {
        return await _context.BatchUploads
            .Where(b => b.UserEmail == userEmail)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}