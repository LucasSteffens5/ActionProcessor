using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class FileQueryHandler(
    IBatchRepository batchRepository,
    IEventRepository eventRepository,
    ILogger<FileQueryHandler> logger)
{
    // TODO: Separar a lógica de consulta de status de lote, lista de lotes e eventos falhados em serviços separados
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

    public async Task<GetFailedEventsResult> HandleAsync(GetFailedEventsQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var failedEvents = await eventRepository.GetFailedEventsAsync(query.BatchId, cancellationToken);

            var failedEventSummaries = failedEvents.Select(evt => new FailedEventSummary(
                evt.Id,
                evt.BatchId,
                evt.Document,
                evt.ClientIdentifier,
                evt.ActionType,
                evt.ErrorMessage ?? "Unknown error",
                evt.RetryCount,
                evt.CompletedAt ?? evt.CreatedAt
            ));

            return new GetFailedEventsResult(failedEventSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting failed events");
            return new GetFailedEventsResult(Enumerable.Empty<FailedEventSummary>());
        }
    }
}
