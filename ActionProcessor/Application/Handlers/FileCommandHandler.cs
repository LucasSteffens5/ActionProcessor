using ActionProcessor.Application.Commands;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Domain.ValueObjects;

namespace ActionProcessor.Application.Handlers;

public class FileCommandHandler(
    IBatchRepository batchRepository,
    IEventRepository eventRepository,
    ILogger<FileCommandHandler> logger)
{
    public async Task<UploadFileResult> HandleAsync(UploadFileCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing file upload: {FileName}", command.File.FileName);

            var batch = new BatchUpload(
                fileName: Guid.NewGuid().ToString(),
                originalFileName: command.File.FileName ?? "Unknown",
                fileSizeBytes: command.File.Length
            );

            // TODO: Enviar para um bucket ou armazenamento de arquivos

            await batchRepository.AddAsync(batch, cancellationToken);

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
                    logger.LogWarning("Failed to parse line {LineNumber}: {Error}", lineNumber, ex.Message);
                    // Continue processing other lines
                }
            }

            if (events.Count == 0)
            {
                batch.Fail("No valid events found in file");
                await batchRepository.UpdateAsync(batch, cancellationToken);
                return new UploadFileResult(batch.Id, batch.OriginalFileName, 0, false, "No valid events found in file");
            }

            // Save events
            await eventRepository.AddRangeAsync(events, cancellationToken);

            // Update batch with total events count
            batch.SetTotalEvents(events.Count);
            await batchRepository.UpdateAsync(batch, cancellationToken);

            logger.LogInformation("File uploaded successfully. BatchId: {BatchId}, Events: {EventCount}",
                batch.Id, events.Count);

            return new UploadFileResult(batch.Id, batch.OriginalFileName, events.Count, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing file upload: {FileName}", command.File.FileName);
            return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, ex.Message);
        }
    }
}
