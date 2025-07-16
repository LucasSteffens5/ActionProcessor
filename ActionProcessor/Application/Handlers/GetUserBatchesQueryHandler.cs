using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class GetUserBatchesQueryHandler(
    IBatchRepository batchRepository,
    ILogger<GetUserBatchesQueryHandler> logger)
{
    public async Task<GetUserBatchesResult> HandleAsync(GetUserBatchesQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query.UserEmail))
            {
                logger.LogWarning("GetUserBatchesQuery called with empty email");
                return new GetUserBatchesResult([]);
            }

            logger.LogInformation("Getting batches for user: {UserEmail}", query.UserEmail);

            var batches = await batchRepository.GetBatchesByEmailOrderedAsync(
                query.UserEmail, 
                query.Skip, 
                query.Take, 
                cancellationToken);

            var userBatchDetails = batches.Select(batch =>
            {
                var progress = batch.GetProgress();
                return new UserBatchDetail(
                    batch.Id,
                    batch.OriginalFileName,
                    batch.Status.ToString(),
                    batch.CreatedAt,
                    batch.StartedAt,
                    batch.CompletedAt,
                    progress.TotalEvents,
                    progress.ProcessedEvents,
                    progress.FailedEvents,
                    batch.IsActive(),
                    batch.HasPendingEvents(),
                    progress.PercentageComplete
                );
            });

            return new GetUserBatchesResult(userBatchDetails);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting batches for user: {UserEmail}", query.UserEmail);
            return new GetUserBatchesResult([]);
        }
    }
}