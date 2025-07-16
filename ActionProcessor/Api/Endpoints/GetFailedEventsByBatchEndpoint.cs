using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

public class GetFailedEventsByBatchEndpoint : IEndpoint
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
            .WithName("GetFailedEventsByBatch")
            .WithSummary("Get failed events for a batch")
            .Produces<GetFailedEventsByBatchResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}