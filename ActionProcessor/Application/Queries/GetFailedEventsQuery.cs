namespace ActionProcessor.Application.Queries;

public sealed record GetFailedEventsQuery(
    Guid? BatchId = null,
    string? UserEmail = null
);