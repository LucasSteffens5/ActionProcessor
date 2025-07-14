using ActionProcessor.Infrastructure.ActionHandlers.SideEffects;
using FluentAssertions;
using Xunit;

namespace ActionProcessor.Tests.Infrastructure.ActionHandlers.Contracts;

public class SampleSideEffectsTests
{
    [Fact]
    public void FromJson_WithValidJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
            {
                "edipi": 123456789,
                "firstName": "John",
                "lastName": "Smith",
                "department": "Engineering",
                "clearanceLevel": "SECRET",
                "email": "john.smith@example.com"
            }
            """;

        // Act
        var result = SampleSideEffects.FromJson(json);

        // Assert
        result.Edipi.Should().Be(123456789);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Smith");
        result.Department.Should().Be("Engineering");
        result.ClearanceLevel.Should().Be("SECRET");
        result.Email.Should().Be("john.smith@example.com");
    }

    [Fact]
    public void FromJson_WithEmptyJson_ShouldReturnEmptyContract()
    {
        // Act
        var result = SampleSideEffects.FromJson("");

        // Assert
        result.Should().NotBeNull();
        result.Edipi.Should().BeNull();
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithNullJson_ShouldReturnEmptyContract()
    {
        // Act
        var result = SampleSideEffects.FromJson(null);

        // Assert
        result.Should().NotBeNull();
        result.Edipi.Should().BeNull();
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldReturnEmptyContract()
    {
        // Act
        var result = SampleSideEffects.FromJson("{ invalid json");

        // Assert
        result.Should().NotBeNull();
        result.Edipi.Should().BeNull();
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
    }

    [Fact]
    public void IsValid_WithEdipi_ShouldReturnTrue()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(Edipi: 123456789);

        // Act
        var result = sideEffects.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithFirstName_ShouldReturnTrue()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(FirstName: "John");

        // Act
        var result = sideEffects.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithLastName_ShouldReturnTrue()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(LastName: "Smith");

        // Act
        var result = sideEffects.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithoutRequiredFields_ShouldReturnFalse()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(Department: "Engineering");

        // Act
        var result = sideEffects.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFullName_WithBothNames_ShouldReturnCombined()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(FirstName: "John", LastName: "Smith");

        // Act
        var result = sideEffects.GetFullName();

        // Assert
        result.Should().Be("John Smith");
    }

    [Fact]
    public void GetFullName_WithOnlyFirstName_ShouldReturnFirstName()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(FirstName: "John");

        // Act
        var result = sideEffects.GetFullName();

        // Assert
        result.Should().Be("John");
    }

    [Fact]
    public void GetFullName_WithOnlyLastName_ShouldReturnLastName()
    {
        // Arrange
        var sideEffects = new SampleSideEffects(LastName: "Smith");

        // Act
        var result = sideEffects.GetFullName();

        // Assert
        result.Should().Be("Smith");
    }

    [Fact]
    public void GetFullName_WithoutNames_ShouldReturnEmpty()
    {
        // Arrange
        var sideEffects = new SampleSideEffects();

        // Act
        var result = sideEffects.GetFullName();

        // Assert
        result.Should().BeEmpty();
    }
}
