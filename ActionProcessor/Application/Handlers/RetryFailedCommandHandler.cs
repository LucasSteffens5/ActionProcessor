using ActionProcessor.Application.Commands;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class RetryFailedCommandHandler(
    IEventRepository eventRepository,
    ILogger<RetryFailedCommandHandler> logger)
{
    public async Task<RetryFailedEventsResult> HandleAsync(RetryFailedEventsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrying failed events for batch: {BatchId}", command.BatchId);

            var failedEvents = await eventRepository.GetFailedEventsAsync(command.BatchId, cancellationToken);

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