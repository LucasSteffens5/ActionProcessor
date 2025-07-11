using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ActionProcessor.Api.Endpoints;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this WebApplication app)
    {
        var fileGroup = app.MapGroup("/api/files")
            .WithTags("Files")
            .WithOpenApi();
        
        fileGroup.MapPost("/upload", UploadFileAsync)
            .WithName("UploadFile")
            .WithSummary("Upload a file for batch processing")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadFileResult>(200)
            .Produces(400);
        
        fileGroup.MapGet("/batches", GetBatchesAsync)
            .WithName("GetBatches")
            .WithSummary("Get list of all batches")
            .Produces<GetBatchListResult>(200);
        
        fileGroup.MapGet("/batches/{batchId:guid}", GetBatchStatusAsync)
            .WithName("GetBatchStatus")
            .WithSummary("Get status of a specific batch")
            .Produces<GetBatchStatusResult>(200)
            .Produces(404);
        
        fileGroup.MapGet("/batches/{batchId:guid}/failed-events", GetFailedEventsAsync)
            .WithName("GetFailedEvents")
            .WithSummary("Get failed events for a batch")
            .Produces<GetFailedEventsResult>(200);
        
        fileGroup.MapPost("/batches/{batchId:guid}/retry", RetryFailedEventsAsync)
            .WithName("RetryFailedEvents")
            .WithSummary("Retry failed events for a batch")
            .Produces<RetryFailedEventsResult>(200)
            .Produces(400);
    }
    
    private static async Task<IResult> UploadFileAsync(
        IFormFile file,
        FileCommandHandler commandHandler,
        IValidator<UploadFileCommand> validator)
    {
        var command = new UploadFileCommand(file);
        
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        
        var result = await commandHandler.HandleAsync(command);
        
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    
    private static async Task<IResult> GetBatchesAsync(
        FileQueryHandler queryHandler,
        int skip = 0,
        int take = 100)
    {
        var query = new GetBatchListQuery(skip, take);
        var result = await queryHandler.HandleAsync(query);
        
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetBatchStatusAsync(
        Guid batchId,
        FileQueryHandler queryHandler)
    {
        var query = new GetBatchStatusQuery(batchId);
        var result = await queryHandler.HandleAsync(query);
        
        if (result == null)
        {
            return Results.NotFound($"Batch {batchId} not found");
        }
        
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetFailedEventsAsync(
        Guid batchId,
        FileQueryHandler queryHandler)
    {
        var query = new GetFailedEventsQuery(batchId);
        var result = await queryHandler.HandleAsync(query);
        
        return Results.Ok(result);
    }
    
    private static async Task<IResult> RetryFailedEventsAsync(
        Guid batchId,
        FileCommandHandler commandHandler,
        Guid[]? eventIds = null)
    {
        var command = new RetryFailedEventsCommand(batchId, eventIds);
        var result = await commandHandler.HandleAsync(command);
        
        if (result.Success)
        {
            return Results.Ok(result);
        }
        
        return Results.BadRequest(result);
    }
}
