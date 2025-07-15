namespace ActionProcessor.Application.Queries;
// TODO: Separar Queries
public record GetBatchStatusQuery(Guid BatchId);

public record GetBatchStatusResult(
    Guid BatchId,
    string FileName,
    string UserEmail,
    string Status,
    int TotalEvents,
    int ProcessedEvents,
    int SuccessfulEvents,
    int FailedEvents,
    decimal PercentageComplete,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage = null
);

public record GetBatchListQuery(
    int Skip = 0,
    int Take = 100,
    string? UserEmail = null
);

public record GetBatchListResult(
    IEnumerable<BatchSummary> Batches
);

public record BatchSummary(
    Guid BatchId,
    string FileName,
    string UserEmail,
    string Status,
    int TotalEvents,
    decimal PercentageComplete,
    DateTime CreatedAt
);

public record GetFailedEventsQuery(
    Guid? BatchId = null,
    string? UserEmail = null
);

public record GetFailedEventsResult(
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
