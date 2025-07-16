using ActionProcessor.Domain.Entities;

namespace ActionProcessor.Domain.Interfaces;

public interface IBatchRepository
{
    Task<BatchUpload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchUpload> AddAsync(BatchUpload batch, CancellationToken cancellationToken = default);
    Task UpdateAsync(BatchUpload batch, CancellationToken cancellationToken = default);
    Task<IEnumerable<BatchUpload>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<BatchUpload>> GetByEmailAsync(string userEmail, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<BatchUpload?> GetActiveBatchByEmailAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<bool> HasPendingEventsByEmailAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<IEnumerable<BatchUpload>> GetBatchesByEmailOrderedAsync(string userEmail, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}