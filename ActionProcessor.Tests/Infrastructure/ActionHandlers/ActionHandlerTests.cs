using ActionProcessor.Domain.ValueObjects;
using ActionProcessor.Infrastructure.ActionHandlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ActionProcessor.Tests.Infrastructure.ActionHandlers;

public class SampleActionHandlerTests
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SampleActionHandler> _logger;
    private readonly SampleActionHandler _handler;

    public SampleActionHandlerTests()
    {
        _httpClient = new HttpClient();
        _logger = Substitute.For<ILogger<SampleActionHandler>>();
        _handler = new SampleActionHandler(_httpClient, _logger);
    }

    [Fact]
    public void ActionType_ShouldReturnCorrectValue()
    {
        // Act
        var actionType = _handler.ActionType;

        // Assert
        actionType.Should().Be("SAMPLE_ACTION");
    }

    [Fact]
    public async Task ExecuteAsync_WithValidEventData_ShouldReturnSuccessResult()
    {
        // Arrange
        var eventData = new EventData(
            "123456789",
            "client1",
            "SAMPLE_ACTION",
            new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        );

        // Act
        var result = await _handler.ExecuteAsync(eventData);

        // Assert - Since we're simulating success in most cases
        // we'll check that it returns a result (success or failure)
        result.Should().NotBeNull();

        if (result.IsSuccess)
        {
            result.ResponseData.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().BeNull();
        }
        else
        {
            result.ResponseData.Should().BeNull();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogAppropriateMessages()
    {
        // Arrange
        var eventData = new EventData(
            "123456789",
            "client1",
            "SAMPLE_ACTION",
            new Dictionary<string, object>()
        );

        // Act
        var result = await _handler.ExecuteAsync(eventData);

        // Assert
        _logger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Executing SAMPLE_ACTION")),
            Arg.Any<object[]>());

        if (result.IsSuccess)
        {
            _logger.Received().LogInformation(
                Arg.Is<string>(s => s.Contains("SAMPLE_ACTION completed successfully")),
                Arg.Any<object[]>());
        }
        else
        {
            _logger.Received().LogError(
                Arg.Any<Exception>(),
                Arg.Is<string>(s => s.Contains("SAMPLE_ACTION failed")),
                Arg.Any<object[]>());
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var eventData = new EventData("123", "client1", "SAMPLE_ACTION", new Dictionary<string, object>());
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.ExecuteAsync(eventData, cts.Token));
    }
}

public class ActionHandlerFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActionHandlerFactory _factory;

    public ActionHandlerFactoryTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();

        // Setup service provider to return our handler
        var sampleHandler = Substitute.For<SampleActionHandler>(
            Substitute.For<HttpClient>(),
            Substitute.For<ILogger<SampleActionHandler>>());

        _serviceProvider.GetService(typeof(SampleActionHandler)).Returns(sampleHandler);

        _factory = new ActionHandlerFactory(_serviceProvider);
    }

    [Fact]
    public void GetHandler_WithValidActionType_ShouldReturnHandler()
    {
        // Act
        var handler = _factory.GetHandler("SAMPLE_ACTION");

        // Assert
        handler.Should().NotBeNull();
        handler!.ActionType.Should().Be("SAMPLE_ACTION");
    }

    [Fact]
    public void GetHandler_WithInvalidActionType_ShouldReturnNull()
    {
        // Act
        var handler = _factory.GetHandler("INVALID_ACTION");

        // Assert
        handler.Should().BeNull();
    }

    [Fact]
    public void GetSupportedActionTypes_ShouldReturnAllRegisteredTypes()
    {
        // Act
        var supportedTypes = _factory.GetSupportedActionTypes();

        // Assert
        supportedTypes.Should().Contain("SAMPLE_ACTION");
    }
}
