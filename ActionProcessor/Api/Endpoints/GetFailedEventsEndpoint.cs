using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetFailedEventsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/files/batches/{batchId:guid}/failed-events", async (
                Guid batchId,
                FileQueryHandler queryHandler) =>
            {
                var query = new GetFailedEventsQuery(batchId);
                var result = await queryHandler.HandleAsync(query);
                
                return Results.Ok(result);
            })
            .WithName("GetFailedEvents")
            .WithSummary("Get failed events for a batch")
            .Produces<GetFailedEventsResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}
