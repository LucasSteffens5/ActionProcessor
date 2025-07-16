namespace ActionProcessor.Application.Queries;

public sealed record GetUserBatchesQuery(
    string UserEmail,
    int Skip = 0,
    int Take = 100
);
