using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetBatchesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/files/batches", async (
                GetBatchListQueryHandler queryHandler,
                int skip = 0,
                int take = 100,
                string? userEmail = null) =>
            {
                var query = new GetBatchListQuery(skip, take, userEmail);
                var result = await queryHandler.HandleAsync(query);

                return Results.Ok(result);
            })
            .WithName("GetBatches")
            .WithSummary("Get list of all batches")
            .Produces<GetBatchListResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}
