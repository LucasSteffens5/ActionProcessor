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

public class GetUserBatchesQueryHandlerTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly ILogger<GetUserBatchesQueryHandler> _logger;
    private readonly GetUserBatchesQueryHandler _handler;

    public GetUserBatchesQueryHandlerTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _logger = Substitute.For<ILogger<GetUserBatchesQueryHandler>>();

        _handler = new GetUserBatchesQueryHandler(
            _batchRepository,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidQuery_ShouldReturnUserBatches()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new GetUserBatchesQuery(userEmail, 0, 10);

        var batch1 = new BatchUpload("file1.csv", "original1.csv", 1000, userEmail);
        var batch2 = new BatchUpload("file2.csv", "original2.csv", 2000, userEmail);
        
        // Simulate some events for progress calculation
        batch1.SetTotalEvents(10);
        batch2.SetTotalEvents(5);

        var batches = new List<BatchUpload> { batch1, batch2 };

        _batchRepository.GetBatchesByEmailOrderedAsync(userEmail, 0, 10, Arg.Any<CancellationToken>())
            .Returns(batches);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().HaveCount(2);
        
        var firstBatch = result.Batches.First();
        firstBatch.OriginalFileName.Should().Be("original1.csv");
        firstBatch.Status.Should().Be("Uploaded");
        firstBatch.TotalEvents.Should().Be(10);
        firstBatch.IsActive.Should().BeTrue();
        
        var secondBatch = result.Batches.Last();
        secondBatch.OriginalFileName.Should().Be("original2.csv");
        secondBatch.TotalEvents.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEmail_ShouldReturnEmptyResult()
    {
        // Arrange
        var query = new GetUserBatchesQuery("", 0, 10);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().BeEmpty();
        
        // Verify repository was not called
        await _batchRepository.DidNotReceive().GetBatchesByEmailOrderedAsync(
            Arg.Any<string>(), 
            Arg.Any<int>(), 
            Arg.Any<int>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceEmail_ShouldReturnEmptyResult()
    {
        // Arrange
        var query = new GetUserBatchesQuery("   ", 0, 10);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldReturnEmptyResult()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new GetUserBatchesQuery(userEmail, 0, 10);

        _batchRepository.GetBatchesByEmailOrderedAsync(userEmail, 0, 10, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database error"));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNoBatches_ShouldReturnEmptyResult()
    {
        // Arrange
        var userEmail = "user@example.com";
        var query = new GetUserBatchesQuery(userEmail, 0, 10);

        _batchRepository.GetBatchesByEmailOrderedAsync(userEmail, 0, 10, Arg.Any<CancellationToken>())
            .Returns(new List<BatchUpload>());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCorrectParametersToRepository()
    {
        // Arrange
        var userEmail = "user@example.com";
        var skip = 20;
        var take = 50;
        var query = new GetUserBatchesQuery(userEmail, skip, take);

        _batchRepository.GetBatchesByEmailOrderedAsync(userEmail, skip, take, Arg.Any<CancellationToken>())
            .Returns(new List<BatchUpload>());

        // Act
        await _handler.HandleAsync(query);

        // Assert
        await _batchRepository.Received(1).GetBatchesByEmailOrderedAsync(
            userEmail,
            skip,
            take,
            Arg.Any<CancellationToken>());
    }
}
