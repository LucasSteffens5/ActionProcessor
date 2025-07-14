using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class GetBatchStatusQueryHandler(
    IBatchRepository batchRepository,
    ILogger<GetBatchStatusQueryHandler> logger)
{
    public async Task<GetBatchStatusResult?> HandleAsync(GetBatchStatusQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await batchRepository.GetByIdAsync(query.BatchId, cancellationToken);

            if (batch == null)
                return null;

            var progress = batch.GetProgress();

            return new GetBatchStatusResult(
                progress.BatchId,
                batch.OriginalFileName,
                progress.Status.ToString(),
                progress.TotalEvents,
                progress.ProcessedEvents,
                progress.SuccessfulEvents,
                progress.FailedEvents,
                progress.PercentageComplete,
                progress.CreatedAt,
                progress.StartedAt,
                progress.CompletedAt,
                batch.ErrorMessage
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting batch status: {BatchId}", query.BatchId);
            return null;
        }
    }
}
