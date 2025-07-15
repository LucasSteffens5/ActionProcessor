using ActionProcessor.Application.Commands;
using ActionProcessor.Application.Handlers;
using ActionProcessor.Domain.Entities;
using ActionProcessor.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
        var command = new UploadFileCommand(file, "test@example.com");

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
        var command = new UploadFileCommand(file, "test@example.com");

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
        var command = new UploadFileCommand(file, "test@example.com");

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

    private static IFormFile CreateMockFormFile(string fileName, string content)
    {
        var file = Substitute.For<IFormFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        file.FileName.Returns(fileName);
        file.Length.Returns(stream.Length);
        file.OpenReadStream().Returns(stream);

        return file;
    }
}
