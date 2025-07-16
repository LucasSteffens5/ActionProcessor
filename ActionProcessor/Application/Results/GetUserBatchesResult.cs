namespace ActionProcessor.Application.Results;

public sealed record GetUserBatchesResult(
    IEnumerable<UserBatchDetail> Batches
);

public sealed record UserBatchDetail(
    Guid Id,
    string OriginalFileName,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int TotalEvents,
    int ProcessedEvents,
    int FailedEvents,
    bool IsActive,
    bool HasPendingEvents,
    decimal PercentageComplete
);
