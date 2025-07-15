using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;

namespace ActionProcessor.Api.Endpoints;

internal sealed class RetryFailedEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/files/batches/{batchId:guid}/retry", async (
                Guid batchId,
                RetryEventsFailedCommandHandler commandHandler,
                Guid[]? eventIds = null) =>
            {
                var command = new RetryFailedEventsCommand(batchId, eventIds);
                var result = await commandHandler.HandleAsync(command);

                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("RetryFailedEvents")
            .WithSummary("Retry failed events for a batch")
            .Produces<RetryFailedEventsResult>(200)
            .Produces(400)
            .WithTags(Tags.Tags.Files);
    }
}
