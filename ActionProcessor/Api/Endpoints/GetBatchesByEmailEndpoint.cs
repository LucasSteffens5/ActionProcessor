using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Results;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetBatchesByEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/files/batches/by-email/{userEmail}", async (
                string userEmail,
                GetBatchListQueryHandler queryHandler,
                int skip = 0,
                int take = 100) =>
            {
                var query = new GetBatchListQuery(skip, take, userEmail);
                var result = await queryHandler.HandleAsync(query);

                return Results.Ok(result);
            })
            .WithName("GetBatchesByEmail")
            .WithSummary("Get batches by user email")
            .Produces<GetBatchListResult>(200)
            .WithTags(Tags.Tags.Files);
    }
}
