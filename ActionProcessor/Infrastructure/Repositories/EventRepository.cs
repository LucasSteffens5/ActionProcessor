using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Repositories;

public class EventRepository(ActionProcessorDbContext context) : IEventRepository
{
    public async Task<ProcessingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.ProcessingEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);


    public async Task<IEnumerable<ProcessingEvent>> AddRangeAsync(IEnumerable<ProcessingEvent> events,
        CancellationToken cancellationToken = default)
    {
        context.ProcessingEvents.AddRange(events);
        await context.SaveChangesAsync(cancellationToken);
        return events;
    }

    public async Task UpdateAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default)
    {
        context.ProcessingEvents.Update(processingEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProcessingEvent>> GetPendingEventsAsync(int limit = 10,
        CancellationToken cancellationToken = default)
        => await context.ProcessingEvents
            .FromSql($"""
                      
                                      SELECT * FROM "processing_events" 
                                      WHERE "status" = 0 
                                      ORDER BY "created_at" 
                                      LIMIT {limit} 
                                      FOR UPDATE SKIP LOCKED
                      """)
            .ToListAsync(cancellationToken);


    public async Task<IEnumerable<ProcessingEvent>> GetEventsByBatchIdAsync(Guid batchId,
        CancellationToken cancellationToken = default)
        => await context.ProcessingEvents
            .Where(e => e.BatchId == batchId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);


    public async Task<IEnumerable<ProcessingEvent>> GetFailedEventsAsync(Guid? batchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.ProcessingEvents
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