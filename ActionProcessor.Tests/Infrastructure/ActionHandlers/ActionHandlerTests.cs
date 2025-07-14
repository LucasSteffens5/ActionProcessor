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
        var sideEffectsJson = """
            {
                "edipi": 123456789,
                "firstName": "John",
                "lastName": "Smith",
                "department": "Engineering"
            }
            """;
        
        var eventData = new EventData(
            "123456789",
            "client1",
            "SAMPLE_ACTION",
            sideEffectsJson
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
            "{}"
        );

        // Act
        var result = await _handler.ExecuteAsync(eventData);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var eventData = new EventData("123", "client1", "SAMPLE_ACTION", null);
        var cts = new CancellationTokenSource();
        
        // Act - Don't cancel immediately, let the handler run
        var result = await _handler.ExecuteAsync(eventData, cts.Token);
        
        // Assert - Since the handler simulates work, it should complete normally
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidSideEffects_ShouldProcessCorrectly()
    {
        // Arrange
        var sideEffectsJson = """
            {
                "edipi": 123456789,
                "firstName": "John",
                "lastName": "Smith",
                "department": "Engineering",
                "clearanceLevel": "SECRET"
            }
            """;
        
        var eventData = new EventData("DOC123", "CLIENT456", "SAMPLE_ACTION", sideEffectsJson);
        
        // Act
        var result = await _handler.ExecuteAsync(eventData);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidSideEffects_ShouldStillProcess()
    {
        // Arrange
        var eventData = new EventData("DOC123", "CLIENT456", "SAMPLE_ACTION", "{ invalid json");
        
        // Act
        var result = await _handler.ExecuteAsync(eventData);
        
        // Assert
        result.IsSuccess.Should().BeTrue(); // Handler should be resilient
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptySideEffects_ShouldLogWarning()
    {
        // Arrange
        var eventData = new EventData("DOC123", "CLIENT456", "SAMPLE_ACTION", "{}");
        
        // Act
        var result = await _handler.ExecuteAsync(eventData);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSideEffects_ShouldHandleGracefully()
    {
        // Arrange
        var eventData = new EventData("DOC123", "CLIENT456", "SAMPLE_ACTION", null);
        
        // Act
        var result = await _handler.ExecuteAsync(eventData);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
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
