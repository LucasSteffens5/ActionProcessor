namespace ActionProcessor.Application.Results;

public sealed record GetBatchStatusResult(
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



