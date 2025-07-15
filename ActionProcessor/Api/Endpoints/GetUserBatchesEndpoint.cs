using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ActionProcessor.Api.Endpoints;

public class GetUserBatchesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user/{email}/batches", HandleAsync)
            .WithTags("User")
            .WithSummary("Get user batches")
            .WithDescription("Get all batches ordered by upload date for a specific user")
            .Produces<IEnumerable<UserBatchResponse>>()
            .Produces<string>(400);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] string email,
        [FromServices] IBatchRepository batchRepository,
        [FromServices] ILogger<GetUserBatchesEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest("Email é obrigatório");
            }

            logger.LogInformation("Getting batches for user: {UserEmail}", email);

            var batches = await batchRepository.GetBatchesByEmailOrderedAsync(email, 0, 100, cancellationToken);

            var response = batches.Select(batch => new UserBatchResponse(
                Id: batch.Id,
                OriginalFileName: batch.OriginalFileName,
                Status: batch.Status.ToString(),
                CreatedAt: batch.CreatedAt,
                StartedAt: batch.StartedAt,
                CompletedAt: batch.CompletedAt,
                TotalEvents: batch.TotalEvents,
                ProcessedEvents: batch.Events.Count(e => e.Status is EventStatus.Completed or EventStatus.Failed),
                FailedEvents: batch.Events.Count(e => e.Status == EventStatus.Failed),
                IsActive: batch.IsActive(),
                HasPendingEvents: batch.HasPendingEvents()
            ));

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting batches for user: {UserEmail}", email);
            return Results.Problem("Erro interno do servidor");
        }
    }
}

public record UserBatchResponse(
    Guid Id,
    string OriginalFileName,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int TotalEvents,
    int ProcessedEvents,
    int FailedEvents,
    bool IsActive,
    bool HasPendingEvents
);
