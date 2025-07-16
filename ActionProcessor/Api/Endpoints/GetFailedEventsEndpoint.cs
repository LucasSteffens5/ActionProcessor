using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetFailedEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/files/batches/{batchId:guid}/failed-events", async (
                Guid batchId,
                GetFailedEventsQueryHandler queryHandler) =>
            {
                var query = new GetFailedEventsQuery(batchId);
                var result = await queryHandler.HandleAsync(query);

                return Results.Ok(result);
            })
            .WithName("GetFailedEvents")
            .WithSummary("Get failed events for a batch")
            .Produces<GetFailedEventsResult>(200)
            .WithTags(Tags.Tags.Files);

        // TODO: Criar um arquivo para este endpoint de forma separada
        app.MapGet("api/files/failed-events", async (
                GetFailedEventsQueryHandler queryHandler,
                string? userEmail = null) =>
            {
                var query = new GetFailedEventsQuery(null, userEmail);
                var result = await queryHandler.HandleAsync(query);

                return Results.Ok(result);
            })
            .WithName("GetFailedEventsByEmail")
            .WithSummary("Get failed events by user email")
            .Produces<GetFailedEventsResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}
