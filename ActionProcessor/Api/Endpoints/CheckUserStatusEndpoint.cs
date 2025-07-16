using ActionProcessor.Application.Queries;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace ActionProcessor.Api.Endpoints;

public class CheckUserStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/user/{email}/status", HandleAsync)
            .WithTags("User")
            .WithSummary("Check user processing status")
            .WithDescription("Verify if user has active batches or pending events in processing")
            .Produces<CheckUserStatusResult>()
            .Produces<string>(400);
    }

    private static async Task<IResult> HandleAsync(
        [FromRoute] string email,
        [FromServices] CheckUserStatusQueryHandler queryHandler,
        [FromServices] ILogger<CheckUserStatusEndpoint> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest("Email é obrigatório");
            }

            var query = new CheckUserStatusQuery(email);
            var result = await queryHandler.HandleAsync(query, cancellationToken);

            if (result == null)
            {
                return Results.BadRequest("Erro ao verificar status do usuário");
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking user status for: {UserEmail}", email);
            return Results.Problem("Erro interno do servidor");
        }
    }
}