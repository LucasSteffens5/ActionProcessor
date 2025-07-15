using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class RetryEventsFailedCommandHandlerValidationTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RetryEventsFailedCommandHandler> _logger;
    private readonly RetryEventsFailedCommandHandler _handler;

    public RetryEventsFailedCommandHandlerValidationTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<RetryEventsFailedCommandHandler>>();
        _handler = new RetryEventsFailedCommandHandler(_eventRepository, _batchRepository, _logger);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasActiveBatch_ShouldReturnError()
    {
        // Arrange
        var userEmail = "test@example.com";
        var batchId = Guid.NewGuid();
        var activeBatchId = Guid.NewGuid();

        var activeBatch = new BatchUpload("active-file.csv", "active-file.csv", 1000, userEmail);
        var targetBatch = new BatchUpload("failed-file.csv", "failed-file.csv", 1000, userEmail);
        targetBatch.Fail("Test error");

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(activeBatch);

        _batchRepository
            .GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(targetBatch);

        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("já possui um arquivo em processamento", result.ErrorMessage);
        Assert.Contains("active-file.csv", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenBatchNotBelongsToUser_ShouldReturnError()
    {
        // Arrange
        var userEmail = "test@example.com";
        var otherUserEmail = "other@example.com";
        var batchId = Guid.NewGuid();

        var batch = new BatchUpload("file.csv", "file.csv", 1000, otherUserEmail);

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository
            .GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("não encontrado ou acesso negado", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenBatchNotFailed_ShouldReturnError()
    {
        // Arrange
        var userEmail = "test@example.com";
        var batchId = Guid.NewGuid();

        var batch = new BatchUpload("file.csv", "file.csv", 1000, userEmail);
        // Status: Uploaded por padrão, não Failed

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository
            .GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Só é possível reprocessar arquivos com status 'Failed'", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsEmpty_ShouldReturnError()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var command = new RetryFailedEventsCommand(batchId, null, "");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email do usuário é obrigatório", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_WhenValidRetryRequest_ShouldProcessSuccessfully()
    {
        // Arrange
        var userEmail = "test@example.com";
        var batchId = Guid.NewGuid();

        var batch = new BatchUpload("file.csv", "file.csv", 1000, userEmail);
        batch.Fail("Test error");

        _batchRepository
            .GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository
            .GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        _eventRepository
            .GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(new List<ProcessingEvent>());

        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.EventsRetried); // Sem eventos para retry
    }
}
