using ActionProcessor.Application.Commands;
using FluentValidation;

namespace ActionProcessor.Api.Validators;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(x => x.File.Length)
            .GreaterThan(0)
            .WithMessage("File cannot be empty")
            .LessThanOrEqualTo(100 * 1024 * 1024) // 100MB limit
            .WithMessage("File size cannot exceed 100MB");

        RuleFor(x => x.File.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .Must(fileName => Path.GetExtension(fileName)?.ToLower() is ".csv" or ".txt")
            .WithMessage("Only CSV and TXT files are supported");
    }
}
