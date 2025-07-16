using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class GetFailedEventsQueryHandler(
    IEventRepository eventRepository,
    ILogger<GetFailedEventsQueryHandler> logger)
{
    public async Task<GetFailedEventsResult> HandleAsync(GetFailedEventsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<ProcessingEvent> failedEvents;
            
            if (!string.IsNullOrWhiteSpace(query.UserEmail))
            {
                failedEvents = await eventRepository.GetFailedEventsByEmailAsync(query.UserEmail, cancellationToken);
            }
            else if (query.BatchId.HasValue)
            {
                failedEvents = await eventRepository.GetFailedEventsAsync(query.BatchId.Value, cancellationToken);
            }
            else
            {
                failedEvents = await eventRepository.GetFailedEventsAsync(null, cancellationToken);
            }

            var failedEventSummaries = failedEvents.Select(evt => new FailedEventSummary(
                evt.Id,
                evt.BatchId,
                evt.Document,
                evt.ClientIdentifier,
                evt.ActionType,
                evt.ErrorMessage ?? "Unknown error",
                evt.RetryCount,
                evt.CompletedAt ?? evt.CreatedAt
            ));

            return new GetFailedEventsResult(failedEventSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting failed events");
            return new GetFailedEventsResult(Enumerable.Empty<FailedEventSummary>());
        }
    }
}
