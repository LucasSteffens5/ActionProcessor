using ActionProcessor.Domain.Entities;

namespace ActionProcessor.Domain.Interfaces;
public interface IEventRepository
{
    Task<ProcessingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> AddRangeAsync(IEnumerable<ProcessingEvent> events, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default);
    Task<List<ProcessingEvent>> GetPendingEventsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetEventsByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetFailedEventsAsync(Guid? batchId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessingEvent>> GetFailedEventsByEmailAsync(string userEmail, CancellationToken cancellationToken = default);
    Task ProcessEventsAsync(int limit, Func<List<ProcessingEvent>, Task> processor, CancellationToken cancellationToken = default);
}

