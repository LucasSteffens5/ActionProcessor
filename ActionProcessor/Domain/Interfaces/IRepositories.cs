using ActionProcessor.Domain.Entities;

namespace ActionProcessor.Domain.Interfaces;

public interface IBatchRepository
{
    Task<BatchUpload?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BatchUpload> AddAsync(BatchUpload batch, CancellationToken cancellationToken = default);
    Task UpdateAsync(BatchUpload batch, CancellationToken cancellationToken = default);
    Task<IEnumerable<BatchUpload>> GetAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
}

public interface IEventRepository
{
    Task<ProcessingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> AddRangeAsync(IEnumerable<ProcessingEvent> events, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetPendingEventsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetEventsByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetFailedEventsAsync(Guid? batchId = null, CancellationToken cancellationToken = default);
}

