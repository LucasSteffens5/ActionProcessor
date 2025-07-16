namespace ActionProcessor.Application.Commands;

public sealed record UploadFileCommand(
    IFormFile File,
    string UserEmail,
    string? SideEffects = null
);