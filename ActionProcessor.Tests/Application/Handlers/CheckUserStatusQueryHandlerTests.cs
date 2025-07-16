using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class CheckUserStatusQueryHandlerTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly ILogger<CheckUserStatusQueryHandler> _logger;
    private readonly CheckUserStatusQueryHandler _handler;

    public CheckUserStatusQueryHandlerTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _logger = Substitute.For<ILogger<CheckUserStatusQueryHandler>>();

        _handler = new CheckUserStatusQueryHandler(
            _batchRepository,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidEmailAndActiveBatch_ShouldReturnStatusWithActiveBatch()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new CheckUserStatusQuery(userEmail);

        var activeBatch = new BatchUpload("file.csv", "original.csv", 1000, userEmail);
        activeBatch.Start(); // Set status to Processing

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(activeBatch);
        _batchRepository.HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.UserEmail.Should().Be(userEmail);
        result.HasActiveBatch.Should().BeTrue();
        result.ActiveBatchId.Should().Be(activeBatch.Id);
        result.ActiveBatchFileName.Should().Be("original.csv");
        result.ActiveBatchStatus.Should().Be("Processing");
        result.CanUploadNewFile.Should().BeFalse();
        result.Message.Should().Be("Usuário possui arquivo em processamento");
    }

    [Fact]
    public async Task HandleAsync_WithValidEmailAndNoBatch_ShouldReturnStatusCanUpload()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new CheckUserStatusQuery(userEmail);

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);
        _batchRepository.HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.UserEmail.Should().Be(userEmail);
        result.HasActiveBatch.Should().BeFalse();
        result.ActiveBatchId.Should().BeNull();
        result.ActiveBatchFileName.Should().BeNull();
        result.ActiveBatchStatus.Should().BeNull();
        result.HasPendingEvents.Should().BeFalse();
        result.CanUploadNewFile.Should().BeTrue();
        result.Message.Should().Be("Usuário pode enviar um novo arquivo");
    }

    [Fact]
    public async Task HandleAsync_WithPendingEvents_ShouldReturnCannotUpload()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new CheckUserStatusQuery(userEmail);

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);
        _batchRepository.HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.UserEmail.Should().Be(userEmail);
        result.HasActiveBatch.Should().BeFalse();
        result.HasPendingEvents.Should().BeTrue();
        result.CanUploadNewFile.Should().BeFalse();
        result.Message.Should().Be("Usuário possui arquivo em processamento");
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEmail_ShouldReturnNull()
    {
        // Arrange
        var query = new CheckUserStatusQuery("");

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();

        // Verify repository was not called
        await _batchRepository.DidNotReceive().GetActiveBatchByEmailAsync(
            Arg.Any<string>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceEmail_ShouldReturnNull()
    {
        // Arrange
        var query = new CheckUserStatusQuery("   ");

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldReturnNull()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new CheckUserStatusQuery(userEmail);

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryMethods()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new CheckUserStatusQuery(userEmail);

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);
        _batchRepository.HasPendingEventsByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.HandleAsync(query);

        // Assert
        await _batchRepository.Received(1).GetActiveBatchByEmailAsync(
            userEmail,
            Arg.Any<CancellationToken>());
        await _batchRepository.Received(1).HasPendingEventsByEmailAsync(
            userEmail,
            Arg.Any<CancellationToken>());
    }
}
