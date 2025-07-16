namespace ActionProcessor.Application.Results;

public sealed record GetBatchListResult(
    IEnumerable<BatchSummary> Batches
);

public sealed record BatchSummary(
    Guid BatchId,
    string FileName,
    string UserEmail,
    string Status,
    int TotalEvents,
    decimal PercentageComplete,
    DateTime CreatedAt
);