namespace ActionProcessor.Application.Results;

public sealed record RetryFailedEventsResult(
    int EventsRetried,
    bool Success,
    string? ErrorMessage = null
);