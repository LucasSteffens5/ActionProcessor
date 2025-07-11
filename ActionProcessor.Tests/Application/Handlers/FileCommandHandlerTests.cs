using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text;
using Xunit;

namespace ActionProcessor.Tests.Application.Handlers;

public class FileCommandHandlerTests
{
    private readonly IBatchRepository _batchRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<FileCommandHandler> _logger;
    private readonly FileCommandHandler _handler;
    
    public FileCommandHandlerTests()
    {
        _batchRepository = Substitute.For<IBatchRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<FileCommandHandler>>();
        
        _handler = new FileCommandHandler(
            _batchRepository,
            _eventRepository,
            _logger);
    }
    
    [Fact]
    public async Task HandleUploadFileCommand_WithValidFile_ShouldReturnSuccessResult()
    {
        // Arrange
        var fileContent = "123456789,client1,SAMPLE_ACTION\n987654321,client2,SAMPLE_ACTION";
        var file = CreateMockFormFile("test.csv", fileContent);
        var command = new UploadFileCommand(file);
        
        _batchRepository.AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<BatchUpload>());
        
        _eventRepository.AddRangeAsync(Arg.Any<IEnumerable<ProcessingEvent>>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<IEnumerable<ProcessingEvent>>());
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeTrue();
        result.TotalEvents.Should().Be(2);
        result.ErrorMessage.Should().BeNull();
        result.BatchId.Should().NotBeEmpty();
        
        await _batchRepository.Received(1).AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>());
        await _eventRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<ProcessingEvent>>(events => events.Count() == 2), 
            Arg.Any<CancellationToken>());
        await _batchRepository.Received(1).UpdateAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleUploadFileCommand_WithEmptyFile_ShouldReturnFailureResult()
    {
        // Arrange
        var file = CreateMockFormFile("test.csv", "");
        var command = new UploadFileCommand(file);
        
        _batchRepository.AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<BatchUpload>());
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeFalse();
        result.TotalEvents.Should().Be(0);
        result.ErrorMessage.Should().Contain("No valid events found");
        
        await _batchRepository.Received(1).AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>());
        await _batchRepository.Received(1).UpdateAsync(
            Arg.Is<BatchUpload>(b => b.Status == BatchStatus.Failed), 
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleUploadFileCommand_WithInvalidLines_ShouldSkipInvalidLinesAndProcessValid()
    {
        // Arrange
        var fileContent = "123456789,client1,SAMPLE_ACTION\ninvalid line\n987654321,client2,SAMPLE_ACTION";
        var file = CreateMockFormFile("test.csv", fileContent);
        var command = new UploadFileCommand(file);
        
        _batchRepository.AddAsync(Arg.Any<BatchUpload>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<BatchUpload>());
        
        _eventRepository.AddRangeAsync(Arg.Any<IEnumerable<ProcessingEvent>>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<IEnumerable<ProcessingEvent>>());
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeTrue();
        result.TotalEvents.Should().Be(2); // Only valid lines
        
        await _eventRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<ProcessingEvent>>(events => events.Count() == 2), 
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleRetryFailedEventsCommand_WithValidBatchId_ShouldRetryFailedEvents()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var command = new RetryFailedEventsCommand(batchId);
        
        var failedEvents = new List<ProcessingEvent>
        {
            CreateFailedEvent(batchId, 1),
            CreateFailedEvent(batchId, 2),
            CreateFailedEvent(batchId, 5) // This one has too many retries
        };
        
        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(failedEvents);
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeTrue();
        result.EventsRetried.Should().Be(2); // Only events with retryCount < 3
        
        await _eventRepository.Received(2).UpdateAsync(
            Arg.Is<ProcessingEvent>(e => e.Status == EventStatus.Pending), 
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleRetryFailedEventsCommand_WithSpecificEventIds_ShouldRetryOnlySpecifiedEvents()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var event1 = CreateFailedEvent(batchId, 1);
        var event2 = CreateFailedEvent(batchId, 1);
        var event3 = CreateFailedEvent(batchId, 1);
        
        var eventIds = new[] { event1.Id, event3.Id };
        var command = new RetryFailedEventsCommand(batchId, eventIds);
        
        var failedEvents = new List<ProcessingEvent> { event1, event2, event3 };
        
        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .Returns(failedEvents);
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeTrue();
        result.EventsRetried.Should().Be(2); // Only event1 and event3
        
        await _eventRepository.Received(2).UpdateAsync(
            Arg.Is<ProcessingEvent>(e => eventIds.Contains(e.Id) && e.Status == EventStatus.Pending), 
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleRetryFailedEventsCommand_WhenRepositoryThrows_ShouldReturnFailureResult()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var command = new RetryFailedEventsCommand(batchId);
        
        _eventRepository.GetFailedEventsAsync(batchId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));
        
        // Act
        var result = await _handler.HandleAsync(command);
        
        // Assert
        result.Success.Should().BeFalse();
        result.EventsRetried.Should().Be(0);
        result.ErrorMessage.Should().Contain("Database error");
    }
    
    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var file = Substitute.For<IFormFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        file.FileName.Returns(fileName);
        file.Length.Returns(stream.Length);
        file.OpenReadStream().Returns(stream);
        
        return file;
    }
    
    private static ProcessingEvent CreateFailedEvent(Guid batchId, int retryCount)
    {
        var evt = new ProcessingEvent(batchId, "123456789", "client1", "SAMPLE_ACTION");
        
        // Use reflection to set the event to failed state with specific retry count
        evt.Start();
        evt.Fail("Test error");
        
        var retryCountField = typeof(ProcessingEvent).GetField("<RetryCount>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        retryCountField?.SetValue(evt, retryCount);
        
        return evt;
    }
}
