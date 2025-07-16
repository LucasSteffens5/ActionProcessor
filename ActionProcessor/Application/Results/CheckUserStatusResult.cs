namespace ActionProcessor.Application.Results;

public sealed record CheckUserStatusResult(
    string UserEmail,
    bool HasActiveBatch,
    Guid? ActiveBatchId,
    string? ActiveBatchFileName,
    string? ActiveBatchStatus,
    bool HasPendingEvents,
    bool CanUploadNewFile,
    string Message
);
