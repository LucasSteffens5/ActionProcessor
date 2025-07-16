using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

internal sealed class RetryFailedEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/files/batches/{batchId:guid}/retry", async (
                Guid batchId,
                RetryEventsFailedCommandHandler commandHandler,
                Guid[]? eventIds = null,
                string userEmail = "") =>
            {
                var command = new RetryFailedEventsCommand(batchId, eventIds, userEmail);
                var result = await commandHandler.HandleAsync(command);

                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("RetryFailedEvents")
            .WithSummary("Retry failed events for a batch")
            .WithDescription("Retry failed events for a specific batch. Requires user email for validation.")
            .Produces<RetryFailedEventsResult>(200)
            .Produces(400)
            .WithTags(Tags.Tags.Files);
    }
}
