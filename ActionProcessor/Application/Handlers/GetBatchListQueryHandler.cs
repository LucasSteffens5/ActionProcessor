using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class GetBatchListQueryHandler(
    IBatchRepository batchRepository,
    ILogger<GetBatchListQueryHandler> logger)
{
    public async Task<GetBatchListResult> HandleAsync(GetBatchListQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var batches = await batchRepository.GetAllAsync(query.Skip, query.Take, cancellationToken);

            var batchSummaries = batches.Select(batch =>
            {
                var progress = batch.GetProgress();
                return new BatchSummary(
                    batch.Id,
                    batch.OriginalFileName,
                    batch.Status.ToString(),
                    progress.TotalEvents,
                    progress.PercentageComplete,
                    batch.CreatedAt
                );
            });

            return new GetBatchListResult(batchSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting batch list");
            return new GetBatchListResult(Enumerable.Empty<BatchSummary>());
        }
    }
}
