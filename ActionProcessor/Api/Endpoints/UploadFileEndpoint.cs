using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Results;
using FluentValidation;

namespace ActionProcessor.Api.Endpoints;

internal sealed class UploadFileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/files/upload", async (
                IFormFile file,
                string userEmail,
                FileCommandHandler commandHandler,
                IValidator<UploadFileCommand> validator,
                string? sideEffects = null) =>
            {
                var command = new UploadFileCommand(file, userEmail, sideEffects);

                var validationResult = await validator.ValidateAsync(command);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.Errors);
                }

                var result = await commandHandler.HandleAsync(command);

                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .WithName("UploadFile")
            .WithSummary("Upload a file for batch processing")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadFileResult>(200)
            .Produces(400)
            .DisableAntiforgery()
            .WithTags(Tags.Tags.Files);
    }
}
