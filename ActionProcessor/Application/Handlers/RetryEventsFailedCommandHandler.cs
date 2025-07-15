using ActionProcessor.Application.Commands;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class RetryEventsFailedCommandHandler(
    IEventRepository eventRepository,
    IBatchRepository batchRepository,
    ILogger<RetryEventsFailedCommandHandler> logger)
{
    public async Task<RetryFailedEventsResult> HandleAsync(RetryFailedEventsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrying failed events for batch: {BatchId}", command.BatchId);

            IEnumerable<ProcessingEvent> failedEvents;

            //TODO: Melhorar esta logica de busca para retry
            if (!string.IsNullOrWhiteSpace(command.UserEmail))
            {
                var batch = await batchRepository.GetByIdAsync(command.BatchId, cancellationToken);
                if (batch == null || batch.UserEmail != command.UserEmail)
                {
                    logger.LogWarning("Batch {BatchId} not found or does not belong to user {UserEmail}", 
                        command.BatchId, command.UserEmail);
                    return new RetryFailedEventsResult(0, false, "Batch not found or access denied");
                }
            }

            failedEvents = await eventRepository.GetFailedEventsAsync(command.BatchId, cancellationToken);

            if (command.EventIds?.Any() == true)
            {
                var eventIds = command.EventIds.ToHashSet();
                failedEvents = failedEvents.Where(e => eventIds.Contains(e.Id));
            }

            var eventsToRetry = failedEvents.Where(e => e.CanRetry()).ToList();

            foreach (var eventToRetry in eventsToRetry)
            {
                eventToRetry.ResetForRetry();
                await eventRepository.UpdateAsync(eventToRetry, cancellationToken);
            }

            logger.LogInformation("Reset {Count} events for retry", eventsToRetry.Count);

            return new RetryFailedEventsResult(eventsToRetry.Count, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrying failed events for batch: {BatchId}", command.BatchId);
            return new RetryFailedEventsResult(0, false, ex.Message);
        }
    }
}