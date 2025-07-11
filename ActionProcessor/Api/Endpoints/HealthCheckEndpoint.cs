namespace ActionProcessor.Api.Endpoints;

internal sealed class HealthCheckEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("health", () => new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            })
            .WithName("HealthCheck")
            .WithSummary("Health check endpoint")
            .WithTags(Tags.Tags.Health);
    }
}
