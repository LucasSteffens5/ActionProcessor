using ActionProcessor.Application.Handlers;
using ActionProcessor.Application.Queries;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class GetFailedEventsQueryHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetFailedEventsQueryHandler> _logger;
    private readonly GetFailedEventsQueryHandler _handler;

    public GetFailedEventsQueryHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<GetFailedEventsQueryHandler>>();

        _handler = new GetFailedEventsQueryHandler(
            _eventRepository,
            _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidBatchId_ShouldReturnFailedEvents()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var query = new GetFailedEventsQuery(batchId);

        var failedEvents = new List<ProcessingEvent>
        {
            CreateFailedEvent(batchId, "123456789"),
            CreateFailedEvent(batchId, "987654321")
        };

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(failedEvents);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.FailedEvents.Should().HaveCount(2);
        result.FailedEvents.First().Document.Should().Be("123456789");
        result.FailedEvents.Last().Document.Should().Be("987654321");
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldReturnEmptyList()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var query = new GetFailedEventsQuery(batchId);

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ProcessingEvent>>(new Exception("Database error")));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.FailedEvents.Should().BeEmpty();
    }

    private static ProcessingEvent CreateFailedEvent(Guid batchId, string document)
    {
        var evt = new ProcessingEvent(batchId, document, "client1", "SAMPLE_ACTION");
        evt.Start();
        evt.Fail("Test error");
        return evt;
    }
}
