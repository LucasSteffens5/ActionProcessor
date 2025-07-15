using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class GetBatchListQueryHandlerTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly ILogger<GetBatchListQueryHandler> _logger;
    private readonly GetBatchListQueryHandler _handler;

    public GetBatchListQueryHandlerTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _logger = Substitute.For<ILogger<GetBatchListQueryHandler>>();

        _handler = new GetBatchListQueryHandler(
            _batchRepository,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidQuery_ShouldReturnBatchList()
    {
        // Arrange
        var query = new GetBatchListQuery(0, 10);

        var batches = new List<BatchUpload>
        {
            new("file1.csv", "original1.csv", 1000),
            new("file2.csv", "original2.csv", 2000)
        };

        _batchRepository.GetAllAsync(0, 10, Arg.Any<CancellationToken>())
            .Returns(batches);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().HaveCount(2);
        result.Batches.First().FileName.Should().Be("original1.csv");
        result.Batches.Last().FileName.Should().Be("original2.csv");
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetBatchListQuery(0, 10);

        _batchRepository.GetAllAsync(0, 10, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<BatchUpload>>(new Exception("Database error")));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().BeEmpty();
    }
}
