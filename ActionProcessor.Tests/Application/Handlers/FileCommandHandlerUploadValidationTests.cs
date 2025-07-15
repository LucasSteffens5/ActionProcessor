using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class FileCommandHandlerUploadValidationTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<FileCommandHandler> _logger;
    private readonly FileCommandHandler _handler;

    public FileCommandHandlerUploadValidationTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<FileCommandHandler>>();
        _handler = new FileCommandHandler(_batchRepository, _eventRepository, _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasActiveBatch_ShouldReturnError()
    {
        // Arrange
        var userEmail = "test@example.com";
        var activeBatch = new BatchUpload(
            "existing-file.csv",
            "existing-file.csv", 
            1000,
            userEmail
        );
        
        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(activeBatch);

        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("new-file.csv");
        mockFile.ContentType.Returns("text/csv");
        mockFile.Length.Returns(500);

        var command = new UploadFileCommand(
            File: mockFile,
            UserEmail: userEmail
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("já possui um arquivo em processamento", result.ErrorMessage);
        Assert.Contains("existing-file.csv", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasPendingEvents_ShouldReturnError()
    {
        // Arrange
        var userEmail = "test@example.com";

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository
            .HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(true);

        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("new-file.csv");
        mockFile.ContentType.Returns("text/csv");
        mockFile.Length.Returns(500);

        var command = new UploadFileCommand(
            File: mockFile,
            UserEmail: userEmail
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("eventos ainda em processamento", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenUserCanUpload_ShouldProcessSuccessfully()
    {
        // Arrange
        var userEmail = "test@example.com";

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository
            .HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(false);

        _batchRepository
            .AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BatchUpload("test.csv", "test.csv", 100, userEmail)));

        _eventRepository
            .AddRangeAsync(Arg.Any<IEnumerable<ProcessingEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ProcessingEvent>().AsEnumerable()));

        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("valid-file.csv");
        mockFile.ContentType.Returns("text/csv");
        mockFile.Length.Returns(500);
        
        var fileContent = "doc1,client1,UPDATE\ndoc2,client2,DELETE";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        mockFile.OpenReadStream().Returns(stream);

        var command = new UploadFileCommand(
            File: mockFile,
            UserEmail: userEmail
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.BatchId);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsEmpty_ShouldReturnError()
    {
        // Arrange
        var mockFile = Substitute.For<IFormFile>();
        mockFile.FileName.Returns("file.csv");
        mockFile.Length.Returns(100);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test,data"));
        mockFile.OpenReadStream().Returns(stream);
        
        var command = new UploadFileCommand(
            File: mockFile,
            UserEmail: ""
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email do usuário é obrigatório", result.ErrorMessage);
    }
}
