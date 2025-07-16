namespace ActionProcessor.Application.Results;

public sealed record UploadFileResult(
    Guid BatchId,
    string FileName,
    int TotalEvents,
    bool Success,
    string? ErrorMessage = null
);