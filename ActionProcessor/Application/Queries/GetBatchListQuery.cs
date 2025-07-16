namespace ActionProcessor.Application.Queries;

public sealed record GetBatchListQuery(
    int Skip = 0,
    int Take = 100,
    string? UserEmail = null
);