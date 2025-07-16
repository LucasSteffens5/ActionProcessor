using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Results;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class RetryEventsFailedCommandHandler(
    IEventRepository eventRepository,
    IBatchRepository batchRepository,
    ILogger<RetryEventsFailedCommandHandler> logger)
{
    public async Task<RetryFailedEventsResult> HandleAsync(RetryFailedEventsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrying failed events for batch: {BatchId} for user: {UserEmail}", 
                command.BatchId, command.UserEmail);

            // Validação obrigatória do email do usuário
            if (string.IsNullOrWhiteSpace(command.UserEmail))
            {
                return new RetryFailedEventsResult(0, false, "Email do usuário é obrigatório para retry");
            }

            // Verificar se usuário já tem algo em processamento
            var activeBatch = await batchRepository.GetActiveBatchByEmailAsync(command.UserEmail, cancellationToken);
            if (activeBatch != null && activeBatch.Id != command.BatchId)
            {
                logger.LogWarning("Retry blocked - user {UserEmail} has active batch: {ActiveBatchId}, trying to retry: {BatchId}", 
                    command.UserEmail, activeBatch.Id, command.BatchId);
                return new RetryFailedEventsResult(0, false, 
                    $"Você já possui um arquivo em processamento: '{activeBatch.OriginalFileName}'. " +
                    $"Só é possível reprocessar um arquivo por vez.");
            }

            // Validar se o batch pertence ao usuário e buscar detalhes
            var batch = await batchRepository.GetByIdAsync(command.BatchId, cancellationToken);
            if (batch == null || batch.UserEmail != command.UserEmail)
            {
                logger.LogWarning("Batch {BatchId} not found or does not belong to user {UserEmail}", 
                    command.BatchId, command.UserEmail);
                return new RetryFailedEventsResult(0, false, "Arquivo não encontrado ou acesso negado");
            }

            // Só permitir retry se batch está Failed
            if (batch.Status != BatchStatus.Failed)
            {
                logger.LogWarning("Cannot retry batch {BatchId} with status {Status}", command.BatchId, batch.Status);
                return new RetryFailedEventsResult(0, false, 
                    $"Só é possível reprocessar arquivos com status 'Failed'. Status atual: {batch.Status}");
            }

            var failedEvents = await eventRepository.GetFailedEventsAsync(command.BatchId, cancellationToken);

            if (command.EventIds?.Any() == true)
            {
                var eventIds = command.EventIds.ToHashSet();
                failedEvents = failedEvents.Where(e => eventIds.Contains(e.Id));
            }

            var eventsToRetry = failedEvents.Where(e => e.CanRetry()).ToList();

            foreach (var eventToRetry in eventsToRetry)
            {
                eventToRetry.ResetForRetry();
                await eventRepository.UpdateAsync(eventToRetry, cancellationToken);
            }

            logger.LogInformation("Reset {Count} events for retry", eventsToRetry.Count);

            return new RetryFailedEventsResult(eventsToRetry.Count, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrying failed events for batch: {BatchId}", command.BatchId);
            return new RetryFailedEventsResult(0, false, ex.Message);
        }
    }
}