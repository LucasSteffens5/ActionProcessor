namespace ActionProcessor.Application.Results;

public sealed record GetFailedEventsByBatchResult(
    IEnumerable<FailedEventSummary> FailedEvents
);

public record FailedEventSummary(
    Guid EventId,
    Guid BatchId,
    string Document,
    string ClientIdentifier,
    string ActionType,
    string ErrorMessage,
    int RetryCount,
    DateTime FailedAt
);
