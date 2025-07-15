namespace ActionProcessor.Application.Commands;
// TODO: Separar os commands
public record UploadFileCommand(
    IFormFile File,
    string UserEmail,
    string? SideEffects = null
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
    IEnumerable<Guid>? EventIds = null,
    string? UserEmail = null
);

public record RetryFailedEventsResult(
    int EventsRetried,
    bool Success,
    string? ErrorMessage = null
);
