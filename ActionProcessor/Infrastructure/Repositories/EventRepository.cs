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

    public async Task<List<ProcessingEvent>> GetPendingEventsAsync(int limit = 10,
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

    public async Task<IEnumerable<ProcessingEvent>> GetFailedEventsByEmailAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        return await context.ProcessingEvents
            .Include(e => e.Batch)
            .Where(e => e.Status == EventStatus.Failed && e.Batch.UserEmail == userEmail)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessEventsAsync(int limit, Func<List<ProcessingEvent>, Task> processor, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5)); // TODO: Ajustar este tempo conforme necessário
            
            var events = await context.ProcessingEvents
                .FromSql($"""
                          SELECT * FROM "processing_events" 
                          WHERE "status" = 0 
                          ORDER BY "created_at" 
                          LIMIT {limit} 
                          FOR UPDATE SKIP LOCKED
                          """)
                .ToListAsync(cancellationToken);

            if (events.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }
            
            foreach (var evt in events)
            {
                evt.Start();
            }
            await context.SaveChangesAsync(cancellationToken);
            
            await processor(events);
            
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}