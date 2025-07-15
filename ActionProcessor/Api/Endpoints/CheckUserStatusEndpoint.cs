using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ActionProcessor.Api.Endpoints;

public class CheckUserStatusEndpoint : IEndpoint
{
    // TODO: Verificar padronização dos Endpoints
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user/{email}/status", HandleAsync)
            .WithTags("User")
            .WithSummary("Check user processing status")
            .WithDescription("Verify if user has active batches or pending events in processing")
            .Produces<UserStatusResponse>()
            .Produces<string>(400);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] string email,
        [FromServices] IBatchRepository batchRepository,
        [FromServices] ILogger<CheckUserStatusEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest("Email é obrigatório");
            }

            logger.LogInformation("Checking status for user: {UserEmail}", email);

            // Verificar batch ativo
            var activeBatch = await batchRepository.GetActiveBatchByEmailAsync(email, cancellationToken);
            var hasActiveBatch = activeBatch != null;

            // Verificar eventos pendentes
            var hasPendingEvents = await batchRepository.HasPendingEventsByEmailAsync(email, cancellationToken);

            var canUploadNewFile = !hasActiveBatch && !hasPendingEvents;

            var response = new UserStatusResponse(
                UserEmail: email,
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

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking user status for: {UserEmail}", email);
            return Results.Problem("Erro interno do servidor");
        }
    }
}

public record UserStatusResponse(
    string UserEmail,
    bool HasActiveBatch,
    Guid? ActiveBatchId,
    string? ActiveBatchFileName,
    string? ActiveBatchStatus,
    bool HasPendingEvents,
    bool CanUploadNewFile,
    string Message
);