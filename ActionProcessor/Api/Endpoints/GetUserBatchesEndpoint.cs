using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Handlers;
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

    public static async Task<IResult> HandleAsync(
        [FromRoute] string email,
        [FromServices] GetUserBatchesQueryHandler queryHandler,
        [FromServices] ILogger<GetUserBatchesEndpoint> logger,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest("Email é obrigatório");
            }

            var query = new GetUserBatchesQuery(email, skip, take);
            var result = await queryHandler.HandleAsync(query, cancellationToken);

            var response = result.Batches.Select(batch => new UserBatchResponse(
                Id: batch.Id,
                OriginalFileName: batch.OriginalFileName,
                Status: batch.Status,
                CreatedAt: batch.CreatedAt,
                StartedAt: batch.StartedAt,
                CompletedAt: batch.CompletedAt,
                TotalEvents: batch.TotalEvents,
                ProcessedEvents: batch.ProcessedEvents,
                FailedEvents: batch.FailedEvents,
                IsActive: batch.IsActive,
                HasPendingEvents: batch.HasPendingEvents
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
