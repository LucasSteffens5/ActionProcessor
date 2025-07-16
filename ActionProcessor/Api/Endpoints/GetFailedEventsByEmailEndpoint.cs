using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetFailedEventsByEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
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
            .Produces<GetFailedEventsByBatchResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}
