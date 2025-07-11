using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ActionProcessorDbContext _context;
    
    public EventRepository(ActionProcessorDbContext context)
    {
        _context = context;
    }
    
    public async Task<ProcessingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessingEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
    
    public async Task<IEnumerable<ProcessingEvent>> AddRangeAsync(IEnumerable<ProcessingEvent> events, CancellationToken cancellationToken = default)
    {
        _context.ProcessingEvents.AddRange(events);
        await _context.SaveChangesAsync(cancellationToken);
        return events;
    }
    
    public async Task UpdateAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default)
    {
        _context.ProcessingEvents.Update(processingEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<ProcessingEvent>> GetPendingEventsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessingEvents
            .FromSqlRaw(@"
                SELECT * FROM ""ProcessingEvents"" 
                WHERE ""Status"" = 0 
                ORDER BY ""CreatedAt"" 
                LIMIT {0} 
                FOR UPDATE SKIP LOCKED", limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<ProcessingEvent>> GetEventsByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessingEvents
            .Where(e => e.BatchId == batchId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<ProcessingEvent>> GetFailedEventsAsync(Guid? batchId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessingEvents
            .Where(e => e.Status == EventStatus.Failed);
            
        if (batchId.HasValue)
        {
            query = query.Where(e => e.BatchId == batchId.Value);
        }
        
        return await query
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync(cancellationToken);
    }
}
