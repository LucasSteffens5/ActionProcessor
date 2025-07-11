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
        var csvLine = "123456789,client1,SAMPLE_ACTION,key1,value1,key2,value2";

        // Act
        var eventData = EventData.Parse(csvLine);

        // Assert
        eventData.Document.Should().Be("123456789");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("SAMPLE_ACTION");
        eventData.SideEffects.Should().HaveCount(2);
        eventData.SideEffects["key1"].Should().Be("value1");
        eventData.SideEffects["key2"].Should().Be("value2");
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
        eventData.SideEffects.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithIncompleteSideEffects_ShouldIgnoreIncompleteKeyValuePairs()
    {
        // Arrange
        var csvLine = "123456789,client1,SAMPLE_ACTION,key1,value1,key2";

        // Act
        var eventData = EventData.Parse(csvLine);

        // Assert
        eventData.Document.Should().Be("123456789");
        eventData.ClientIdentifier.Should().Be("client1");
        eventData.ActionType.Should().Be("SAMPLE_ACTION");
        eventData.SideEffects.Should().HaveCount(1);
        eventData.SideEffects["key1"].Should().Be("value1");
        eventData.SideEffects.Should().NotContainKey("key2");
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
    public void SerializeSideEffects_ShouldReturnValidJsonString()
    {
        // Arrange
        var sideEffects = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = true
        };
        var eventData = new EventData("123", "client1", "ACTION", sideEffects);

        // Act
        var json = eventData.SerializeSideEffects();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("key1");
        json.Should().Contain("value1");
    }

    [Fact]
    public void SerializeSideEffects_WithEmptyDictionary_ShouldReturnEmptyJsonObject()
    {
        // Arrange
        var eventData = new EventData("123", "client1", "ACTION", new Dictionary<string, object>());

        // Act
        var json = eventData.SerializeSideEffects();

        // Assert
        json.Should().Be("{}");
    }

    [Fact]
    public void DeserializeSideEffects_WithValidJson_ShouldReturnCorrectDictionary()
    {
        // Arrange
        var json = "{\"key1\":\"value1\",\"key2\":42}";

        // Act
        var sideEffects = EventData.DeserializeSideEffects(json);

        // Assert
        sideEffects.Should().HaveCount(2);
        sideEffects.Should().ContainKey("key1");
        sideEffects.Should().ContainKey("key2");
    }

    [Fact]
    public void DeserializeSideEffects_WithInvalidJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var invalidJson = "invalid json";

        // Act
        var sideEffects = EventData.DeserializeSideEffects(invalidJson);

        // Assert
        sideEffects.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeSideEffects_WithNullJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        string? nullJson = null;

        // Act
        var sideEffects = EventData.DeserializeSideEffects(nullJson!);

        // Assert
        sideEffects.Should().BeEmpty();
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
