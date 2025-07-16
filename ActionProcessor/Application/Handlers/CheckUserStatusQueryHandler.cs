using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;
using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Application.Handlers;

public class CheckUserStatusQueryHandler(
    IBatchRepository batchRepository,
    ILogger<CheckUserStatusQueryHandler> logger)
{
    public async Task<CheckUserStatusResult?> HandleAsync(CheckUserStatusQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query.UserEmail))
            {
                logger.LogWarning("CheckUserStatusQuery called with empty email");
                return null;
            }

            logger.LogInformation("Checking status for user: {UserEmail}", query.UserEmail);

            // Verificar batch ativo
            var activeBatch = await batchRepository.GetActiveBatchByEmailAsync(query.UserEmail, cancellationToken);
            var hasActiveBatch = activeBatch != null;

            // Verificar eventos pendentes
            var hasPendingEvents = await batchRepository.HasPendingEventsByEmailAsync(query.UserEmail, cancellationToken);

            var canUploadNewFile = !hasActiveBatch && !hasPendingEvents;

            return new CheckUserStatusResult(
                UserEmail: query.UserEmail,
                HasActiveBatch: hasActiveBatch,
                ActiveBatchId: activeBatch?.Id,
                ActiveBatchFileName: activeBatch?.OriginalFileName,
                ActiveBatchStatus: activeBatch?.Status.ToString(),
                HasPendingEvents: hasPendingEvents,
                CanUploadNewFile: canUploadNewFile,
                Message: canUploadNewFile
                    ? "Usuário pode enviar um novo arquivo"
                    : "Usuário possui arquivo em processamento"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking user status for: {UserEmail}", query.UserEmail);
            return null;
        }
    }
}
