using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Domain.ValueObjects;

namespace ActionProcessor.Infrastructure.BackgroundServices;

public class EventProcessorService(
    IServiceProvider serviceProvider,
    ILogger<EventProcessorService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private readonly int _batchSize = configuration.GetValue("EventProcessor:BatchSize", 10);
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(configuration.GetValue("EventProcessor:PollingIntervalSeconds", 2));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Event Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Event Processor Service");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        logger.LogInformation("Event Processor Service stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var batchRepository = scope.ServiceProvider.GetRequiredService<IBatchRepository>();
        var actionHandlerFactory = scope.ServiceProvider.GetRequiredService<IActionHandlerFactory>();

        await eventRepository.ProcessEventsAsync(_batchSize, async (events) =>
        {
            logger.LogInformation("Processing {Count} pending events with pessimistic lock", events.Count);

            var processingTasks = events.Select(evt => ProcessEventAsync(
                evt, batchRepository, actionHandlerFactory, cancellationToken));

            await Task.WhenAll(processingTasks);
        }, cancellationToken);
    }

    private async Task ProcessEventAsync(
        ProcessingEvent processingEvent,
        IBatchRepository batchRepository,
        IActionHandlerFactory actionHandlerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Processing event {EventId} for document {Document}",
                processingEvent.Id, processingEvent.Document);
            
            await batchRepository.StartProcessingAsync(processingEvent.BatchId, cancellationToken);
            
            var handler = actionHandlerFactory.GetHandler(processingEvent.ActionType);
            if (handler == null)
            {
                processingEvent.Fail($"No handler found for action type: {processingEvent.ActionType}");
                return;
            }
            
            var eventData = new EventData(
                processingEvent.Document,
                processingEvent.ClientIdentifier,
                processingEvent.ActionType,
                processingEvent.SideEffectsJson
            );
            
            var result = await handler.ExecuteAsync(eventData, cancellationToken);
            
            if (result.IsSuccess)
            {
                processingEvent.Complete(result.ResponseData);
                logger.LogDebug("Event {EventId} completed successfully", processingEvent.Id);
            }
            else
            {
                processingEvent.Fail(result.ErrorMessage ?? "Unknown error");
                logger.LogWarning("Event {EventId} failed: {Error}", processingEvent.Id, result.ErrorMessage);
            }
            
            await CheckBatchCompletionAsync(processingEvent.BatchId, batchRepository, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing event {EventId}", processingEvent.Id);
            processingEvent.Fail($"Processing error: {ex.Message}");
        }
    }

    private async Task CheckBatchCompletionAsync(
        Guid batchId,
        IBatchRepository batchRepository,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var batch = await batchRepository.GetByIdAsync(batchId, cancellationToken);
                if (batch?.Status != BatchStatus.Processing) return;

                var newStatus = batch.DetermineCompletionStatus();
                switch (newStatus)
                {
                    case BatchStatus.Processing:
                        return;
                    case BatchStatus.Completed:
                        batch.Complete();
                        break;
                    case BatchStatus.Uploaded:
                    case BatchStatus.Failed:
                    default:
                        batch.Fail("All events failed");
                        break;
                }


                var success = await batchRepository.TryUpdateAsync(batch, cancellationToken);
               
                if (!success) continue;
                
                var progress = batch.GetProgress();
                logger.LogInformation("Batch {BatchId} completed with status {Status}. Success: {Success}, Failed: {Failed}",
                    batchId, newStatus, progress.SuccessfulEvents, progress.FailedEvents);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking batch completion for {BatchId}, attempt {Attempt}", batchId, i + 1);
                if (i == maxRetries - 1) break;
                
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (i + 1)), cancellationToken);
            }
        }
    }
}
