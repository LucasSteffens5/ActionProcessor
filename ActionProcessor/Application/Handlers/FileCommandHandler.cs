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
            logger.LogInformation("Processing file upload: {FileName} for user: {UserEmail}", 
                command.File.FileName, command.UserEmail);

            // Validação 0: Email obrigatório
            if (string.IsNullOrWhiteSpace(command.UserEmail))
            {
                logger.LogWarning("Upload blocked - missing user email");
                return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, 
                    "Email do usuário é obrigatório para envio de arquivo.");
            }

            // Validação 1: Verificar se há batch ativo
            var activeBatch = await batchRepository.GetActiveBatchByEmailAsync(command.UserEmail, cancellationToken);
            if (activeBatch != null)
            {
                logger.LogWarning("Upload blocked - user {UserEmail} has active batch: {BatchId}", 
                    command.UserEmail, activeBatch.Id);
                return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, 
                    $"Você já possui um arquivo em processamento: '{activeBatch.OriginalFileName}'. " +
                    $"Aguarde a conclusão antes de enviar um novo arquivo.");
            }

            // Validação 2: Verificar se há eventos pendentes em outros batches
            var hasPendingEvents = await batchRepository.HasPendingEventsByEmailAsync(command.UserEmail, cancellationToken);
            if (hasPendingEvents)
            {
                logger.LogWarning("Upload blocked - user {UserEmail} has pending events", command.UserEmail);
                return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, 
                    "Você possui eventos ainda em processamento. Aguarde a conclusão de todos os eventos antes de enviar um novo arquivo.");
            }

            var batch = new BatchUpload(
                fileName: Guid.NewGuid().ToString(),
                originalFileName: command.File.FileName ?? "Unknown",
                fileSizeBytes: command.File.Length,
                userEmail: command.UserEmail
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
                        command.SideEffects ?? "{}"
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

            logger.LogInformation("File uploaded successfully. BatchId: {BatchId}, Events: {EventCount} for user: {UserEmail}",
                batch.Id, events.Count, command.UserEmail);

            return new UploadFileResult(batch.Id, batch.OriginalFileName, events.Count, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing file upload: {FileName} for user: {UserEmail}", 
                command.File.FileName, command.UserEmail);
            return new UploadFileResult(Guid.Empty, command.File.FileName ?? "unknown", 0, false, ex.Message);
        }
    }
}
