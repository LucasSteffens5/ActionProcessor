namespace ActionProcessor.Application.Commands;

public record UploadFileCommand(
    IFormFile File
);

public record UploadFileResult(
    Guid BatchId,
    string FileName,
    int TotalEvents,
    bool Success,
    string? ErrorMessage = null
);

public record RetryFailedEventsCommand(
    Guid BatchId,
    IEnumerable<Guid>? EventIds = null
);

public record RetryFailedEventsResult(
    int EventsRetried,
    bool Success,
    string? ErrorMessage = null
);
