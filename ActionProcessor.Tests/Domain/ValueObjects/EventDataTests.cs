using ActionProcessor.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ActionProcessor.Tests.Domain.ValueObjects;

public class EventDataTests
{
    [Fact]
    public void Parse_WithValidCsvLine_ShouldCreateCorrectEventData()
    {
        // Arrange
        var csvLine = "123456789,client1,SAMPLE_ACTION";

        // Act
        var eventData = EventData.Parse(csvLine);

        // Assert
        eventData.Document.Should().Be("123456789");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("SAMPLE_ACTION");
        eventData.SideEffectsJson.Should().BeNull();
    }

    [Fact]
    public void Parse_WithMinimalCsvLine_ShouldCreateEventDataWithoutSideEffects()
    {
        // Arrange
        var csvLine = "123456789,client1,SAMPLE_ACTION";

        // Act
        var eventData = EventData.Parse(csvLine);

        // Assert
        eventData.Document.Should().Be("123456789");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("SAMPLE_ACTION");
        eventData.SideEffectsJson.Should().BeNull();
    }

    [Fact]
    public void Parse_WithSpacesInValues_ShouldTrimCorrectly()
    {
        // Arrange
        var csvLine = " 123456789 , client1 , SAMPLE_ACTION ";

        // Act
        var eventData = EventData.Parse(csvLine);

        // Assert
        eventData.Document.Should().Be("123456789");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("SAMPLE_ACTION");
    }

    [Fact]
    public void Parse_WithInvalidCsvLine_ShouldThrowArgumentException()
    {
        // Arrange
        var csvLine = "123456789,client1"; // Missing action type

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventData.Parse(csvLine));
    }

    [Fact]
    public void Parse_WithEmptyLine_ShouldThrowArgumentException()
    {
        // Arrange
        var csvLine = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EventData.Parse(csvLine));
    }

    [Fact]
    public void Constructor_WithSideEffectsJson_ShouldSetCorrectly()
    {
        // Arrange
        var sideEffectsJson = """{"edipi": 123456789, "firstName": "John"}""";

        // Act
        var eventData = new EventData("123", "client1", "ACTION", sideEffectsJson);

        // Assert
        eventData.Document.Should().Be("123");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("ACTION");
        eventData.SideEffectsJson.Should().Be(sideEffectsJson);
    }

    [Fact]
    public void Constructor_WithoutSideEffectsJson_ShouldDefaultToNull()
    {
        // Act
        var eventData = new EventData("123", "client1", "ACTION");

        // Assert
        eventData.Document.Should().Be("123");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("ACTION");
        eventData.SideEffectsJson.Should().BeNull();
    }
}

public class ActionResultTests
{
    [Fact]
    public void Success_WithResponseData_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var responseData = "Success response";

        // Act
        var result = ActionResult.Success(responseData);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ResponseData.Should().Be(responseData);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Success_WithoutResponseData_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = ActionResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ResponseData.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = ActionResult.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ResponseData.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
    }
}
