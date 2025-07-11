using ActionProcessor.Application.Commands;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Domain.ValueObjects;
using System.Text.Json;

namespace ActionProcessor.Application.Handlers;

public class FileCommandHandler
{
    private readonly IBatchRepository _batchRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<FileCommandHandler> _logger;
    
    public FileCommandHandler(
        IBatchRepository batchRepository,
        IEventRepository eventRepository,
        ILogger<FileCommandHandler> logger)
    {
        _batchRepository = batchRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }
    
    public async Task<UploadFileResult> HandleAsync(UploadFileCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing file upload: {FileName}", command.File.FileName);
            
            // Create batch
            var batch = new BatchUpload(
                fileName: Guid.NewGuid().ToString(),
                originalFileName: command.File.FileName ?? "unknown",
                fileSizeBytes: command.File.Length
            );
            
            await _batchRepository.AddAsync(batch, cancellationToken);
            
            // Parse file and create events
            var events = new List<ProcessingEvent>();
            using var reader = new StreamReader(command.File.OpenReadStream());
            
            string? line;
            var lineNumber = 0;
            
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                lineNumber++;
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                try
                {
                    var eventData = EventData.Parse(line);
                    var processingEvent = new ProcessingEvent(
                        batch.Id,
                        eventData.Document,
                        eventData.ClientIdentifier,
                        eventData.ActionType,
                        eventData.SerializeSideEffects()
                    );
                    
                    events.Add(processingEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse line {LineNumber}: {Error}", lineNumber, ex.Message);
                    // Continue processing other lines
                }
            }
            
            if (events.Count == 0)
            {
                batch.Fail("No valid events found in file");
                await _batchRepository.UpdateAsync(batch, cancellationToken);
                return new UploadFileResult(batch.Id, batch.OriginalFileName, 0, false, "No valid events found in file");
            }
            
            // Save events
            await _eventRepository.AddRangeAsync(events, cancellationToken);
            
            // Update batch with total events count
            batch.SetTotalEvents(events.Count);
            await _batchRepository.UpdateAsync(batch, cancellationToken);
            
            
            _logger.LogInformation("File uploaded successfully. BatchId: {BatchId}, Events: {EventCount}", 
                batch.Id, events.Count);
            
            return new UploadFileResult(batch.Id, batch.OriginalFileName, events.Count, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file upload: {FileName}", command.File.FileName);
            return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, ex.Message);
        }
    }
    
    public async Task<RetryFailedEventsResult> HandleAsync(RetryFailedEventsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrying failed events for batch: {BatchId}", command.BatchId);
            
            var failedEvents = await _eventRepository.GetFailedEventsAsync(command.BatchId, cancellationToken);
            
            if (command.EventIds?.Any() == true)
            {
                var eventIds = command.EventIds.ToHashSet();
                failedEvents = failedEvents.Where(e => eventIds.Contains(e.Id));
            }
            
            var eventsToRetry = failedEvents.Where(e => e.CanRetry()).ToList();
            
            foreach (var eventToRetry in eventsToRetry)
            {
                eventToRetry.ResetForRetry();
                await _eventRepository.UpdateAsync(eventToRetry, cancellationToken);
            }
            
            _logger.LogInformation("Reset {Count} events for retry", eventsToRetry.Count);
            
            return new RetryFailedEventsResult(eventsToRetry.Count, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed events for batch: {BatchId}", command.BatchId);
            return new RetryFailedEventsResult(0, false, ex.Message);
        }
    }
}
