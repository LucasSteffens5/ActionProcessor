using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class GetBatchStatusQueryHandlerTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly ILogger<GetBatchStatusQueryHandler> _logger;
    private readonly GetBatchStatusQueryHandler _handler;

    public GetBatchStatusQueryHandlerTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _logger = Substitute.For<ILogger<GetBatchStatusQueryHandler>>();

        _handler = new GetBatchStatusQueryHandler(
            _batchRepository,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidBatchId_ShouldReturnBatchStatus()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var query = new GetBatchStatusQuery(batchId);

        var batch = new BatchUpload("test-file.csv", "original-test.csv", 1000, "test@example.com");
        batch.SetTotalEvents(10);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.BatchId.Should().Be(batch.Id);
        result.FileName.Should().Be("original-test.csv");
        result.TotalEvents.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentBatchId_ShouldReturnNull()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var query = new GetBatchStatusQuery(batchId);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldReturnNull()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var query = new GetBatchStatusQuery(batchId);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BatchUpload?>(new Exception("Database error")));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }
}
