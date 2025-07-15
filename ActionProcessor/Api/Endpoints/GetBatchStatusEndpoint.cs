using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;

namespace ActionProcessor.Api.Endpoints;

internal sealed class GetBatchStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/files/batches/{batchId:guid}", async (
                Guid batchId,
                GetBatchStatusQueryHandler queryHandler) =>
            {
                var query = new GetBatchStatusQuery(batchId);
                var result = await queryHandler.HandleAsync(query);

                if (result == null)
                {
                    return Results.NotFound($"Batch {batchId} not found");
                }

                return Results.Ok(result);
            })
            .WithName("GetBatchStatus")
            .WithSummary("Get status of a specific batch")
            .Produces<GetBatchStatusResult>(200)
            .Produces(404)
            .WithTags(Tags.Tags.Files);
    }
}
