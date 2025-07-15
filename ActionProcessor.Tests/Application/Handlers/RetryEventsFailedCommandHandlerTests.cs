using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class RetryEventsFailedCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IBatchRepository _batchRepository;
    private readonly ILogger<RetryEventsFailedCommandHandler> _logger;
    private readonly RetryEventsFailedCommandHandler _handler;

    public RetryEventsFailedCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _batchRepository = Substitute.For<IBatchRepository>();
        _logger = Substitute.For<ILogger<RetryEventsFailedCommandHandler>>();

        _handler = new RetryEventsFailedCommandHandler(
            _eventRepository,
            _batchRepository,
            _logger);
    }

    [Fact]
    public async Task HandleRetryFailedEventsCommand_WithValidBatchId_ShouldRetryFailedEvents()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        var batch = new BatchUpload("test.csv", "test.csv", 1000, userEmail);
        batch.Fail("Test error");

        var failedEvents = new List<ProcessingEvent>
        {
            CreateFailedEvent(batchId, 1),
            CreateFailedEvent(batchId, 2),
            CreateFailedEvent(batchId, 5)
        };

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(failedEvents);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.EventsRetried.Should().Be(2);

        await _eventRepository.Received(2).UpdateAsync(
            Arg.Is<ProcessingEvent>(e => e.Status == EventStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRetryFailedEventsCommand_WithSpecificEventIds_ShouldRetryOnlySpecifiedEvents()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var event1 = CreateFailedEvent(batchId, 1);
        var event2 = CreateFailedEvent(batchId, 1);
        var event3 = CreateFailedEvent(batchId, 1);

        var eventIds = new[] { event1.Id, event3.Id };
        var command = new RetryFailedEventsCommand(batchId, eventIds, userEmail);

        var batch = new BatchUpload("test.csv", "test.csv", 1000, userEmail);
        batch.Fail("Test error");

        var failedEvents = new List<ProcessingEvent> { event1, event2, event3 };

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(failedEvents);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.EventsRetried.Should().Be(2);

        await _eventRepository.Received(2).UpdateAsync(
            Arg.Is<ProcessingEvent>(e => eventIds.Contains(e.Id) && e.Status == EventStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRetryFailedEventsCommand_WhenRepositoryThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        var batch = new BatchUpload("test.csv", "test.csv", 1000, userEmail);
        batch.Fail("Test error");

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeFalse();
        result.EventsRetried.Should().Be(0);
        result.ErrorMessage.Should().Contain("Database error");
    }

    [Fact]
    public async Task HandleRetryFailedEventsCommand_WithNoFailedEvents_ShouldReturnZeroEventsRetried()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var userEmail = "test@example.com";
        var command = new RetryFailedEventsCommand(batchId, null, userEmail);

        var batch = new BatchUpload("test.csv", "test.csv", 1000, userEmail);
        batch.Fail("Test error");

        _batchRepository.GetActiveBatchByEmailAsync(userEmail, Arg.Any<CancellationToken>())
            .Returns((BatchUpload?)null);

        _batchRepository.GetByIdAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(batch);

        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(new List<ProcessingEvent>());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
        result.EventsRetried.Should().Be(0);
    }

    private static ProcessingEvent CreateFailedEvent(Guid batchId, int retryCount)
    {
        var evt = new ProcessingEvent(batchId, "123456789", "client1", "SAMPLE_ACTION");
        
        evt.Start();
        evt.Fail("Test error");

        var retryCountField = typeof(ProcessingEvent).GetField("<RetryCount>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        retryCountField?.SetValue(evt, retryCount);

        return evt;
    }
}
