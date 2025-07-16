namespace ActionProcessor.Application.Commands;

public sealed record RetryFailedEventsCommand(
    Guid BatchId,
    IEnumerable<Guid>? EventIds = null,
    string UserEmail = ""
);