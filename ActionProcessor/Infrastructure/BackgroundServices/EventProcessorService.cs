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

        var pendingEvents = await eventRepository.GetPendingEventsAsync(_batchSize, cancellationToken);

        if (!pendingEvents.Any())
            return;

        logger.LogInformation("Processing {Count} pending events", pendingEvents.Count());

        var processingTasks = pendingEvents.Select(evt => ProcessEventAsync(
            evt, eventRepository, batchRepository, actionHandlerFactory, cancellationToken));

        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessEventAsync(
        ProcessingEvent processingEvent,
        IEventRepository eventRepository,
        IBatchRepository batchRepository,
        IActionHandlerFactory actionHandlerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Processing event {EventId} for document {Document}",
                processingEvent.Id, processingEvent.Document);

            // Mark as processing
            processingEvent.Start();
            await eventRepository.UpdateAsync(processingEvent, cancellationToken);

            // Get action handler
            var handler = actionHandlerFactory.GetHandler(processingEvent.ActionType);
            if (handler == null)
            {
                processingEvent.Fail($"No handler found for action type: {processingEvent.ActionType}");
                await eventRepository.UpdateAsync(processingEvent, cancellationToken);
                return;
            }

            // Prepare event data
            var eventData = new EventData(
                processingEvent.Document,
                processingEvent.ClientIdentifier,
                processingEvent.ActionType,
                processingEvent.SideEffectsJson
            );

            // Execute action
            var result = await handler.ExecuteAsync(eventData, cancellationToken);

            // Update event status
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

            await eventRepository.UpdateAsync(processingEvent, cancellationToken);

            // Check if batch is complete
            await CheckBatchCompletionAsync(processingEvent.BatchId, batchRepository, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing event {EventId}", processingEvent.Id);

            try
            {
                processingEvent.Fail($"Processing error: {ex.Message}");
                await eventRepository.UpdateAsync(processingEvent, cancellationToken);
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Error updating failed event {EventId}", processingEvent.Id);
            }
        }
    }

    private async Task CheckBatchCompletionAsync(
        Guid batchId,
        IBatchRepository batchRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var batch = await batchRepository.GetByIdAsync(batchId, cancellationToken);
            if (batch is not { Status: BatchStatus.Processing })
                return;

            var progress = batch.GetProgress();

            // Check if all events are processed
            if (progress.ProcessedEvents >= progress.TotalEvents)
            {
                batch.Complete();
                await batchRepository.UpdateAsync(batch, cancellationToken);

                logger.LogInformation("Batch {BatchId} completed. Success: {Success}, Failed: {Failed}",
                    batchId, progress.SuccessfulEvents, progress.FailedEvents);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking batch completion for {BatchId}", batchId);
        }
    }
}
