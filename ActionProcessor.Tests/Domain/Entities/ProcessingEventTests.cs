using ActionProcessor.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ActionProcessor.Tests.Domain.Entities;

public class ProcessingEventTests
{
    [Fact]
    public void Constructor_ShouldCreateEventWithCorrectProperties()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var document = "123456789";
        var clientIdentifier = "client1";
        var actionType = "SAMPLE_ACTION";
        var sideEffectsJson = "{\"key\":\"value\"}";

        // Act
        var evt = new ProcessingEvent(batchId, document, clientIdentifier, actionType, sideEffectsJson);

        // Assert
        evt.Id.Should().NotBeEmpty();
        evt.BatchId.Should().Be(batchId);
        evt.Document.Should().Be(document);
        evt.ClientIdentifier.Should().Be(clientIdentifier);
        evt.ActionType.Should().Be(actionType);
        evt.SideEffectsJson.Should().Be(sideEffectsJson);
        evt.Status.Should().Be(EventStatus.Pending);
        evt.RetryCount.Should().Be(0);
        evt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        evt.StartedAt.Should().BeNull();
        evt.CompletedAt.Should().BeNull();
        evt.ErrorMessage.Should().BeNull();
        evt.ResponseData.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaultSideEffects_ShouldUseEmptyJson()
    {
        // Arrange
        var batchId = Guid.NewGuid();

        // Act
        var evt = new ProcessingEvent(batchId, "123", "client1", "ACTION");

        // Assert
        evt.SideEffectsJson.Should().Be("{}");
    }

    [Fact]
    public void Start_WhenStatusIsPending_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");

        // Act
        evt.Start();

        // Assert
        evt.Status.Should().Be(EventStatus.Processing);
        evt.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Start_WhenStatusIsFailed_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();
        evt.Fail("Test error");

        // Act
        evt.Start();

        // Assert
        evt.Status.Should().Be(EventStatus.Processing);
        evt.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Start_WhenStatusIsCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();
        evt.Complete();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => evt.Start());
    }

    [Fact]
    public void Complete_WhenStatusIsProcessing_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();
        var responseData = "Success response";

        // Act
        evt.Complete(responseData);

        // Assert
        evt.Status.Should().Be(EventStatus.Completed);
        evt.ResponseData.Should().Be(responseData);
        evt.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        evt.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Complete_WhenStatusIsNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => evt.Complete());
    }

    [Fact]
    public void Fail_WhenStatusIsProcessing_ShouldChangeStatusToFailedAndIncrementRetryCount()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();
        var errorMessage = "Test error";

        // Act
        evt.Fail(errorMessage);

        // Assert
        evt.Status.Should().Be(EventStatus.Failed);
        evt.ErrorMessage.Should().Be(errorMessage);
        evt.RetryCount.Should().Be(1);
        evt.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ResetForRetry_WhenStatusIsFailed_ShouldResetToPending()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();
        evt.Fail("Test error");

        // Act
        evt.ResetForRetry();

        // Assert
        evt.Status.Should().Be(EventStatus.Pending);
        evt.StartedAt.Should().BeNull();
        evt.CompletedAt.Should().BeNull();
        evt.ErrorMessage.Should().BeNull();
        evt.RetryCount.Should().Be(1); // Should preserve retry count
    }

    [Fact]
    public void ResetForRetry_WhenStatusIsNotFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => evt.ResetForRetry());
    }

    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(1, 3, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, false)]
    [InlineData(4, 3, false)]
    public void CanRetry_ShouldReturnCorrectValue(int retryCount, int maxRetries, bool expected)
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");
        evt.Start();

        // Simulate retries - the retryCount represents how many times the event has failed
        for (int i = 0; i < retryCount; i++)
        {
            evt.Fail("Test error");
            if (i < retryCount - 1) // Don't reset on the last failure
            {
                evt.ResetForRetry();
                evt.Start();
            }
        }

        // If retryCount is 0, we need at least one failure to test CanRetry
        if (retryCount == 0)
        {
            evt.Fail("Test error");
        }

        // Act
        var canRetry = evt.CanRetry(maxRetries);

        // Assert
        canRetry.Should().Be(expected);
    }

    [Fact]
    public void CanRetry_WhenStatusIsNotFailed_ShouldReturnFalse()
    {
        // Arrange
        var evt = new ProcessingEvent(Guid.NewGuid(), "123", "client1", "ACTION");

        // Act
        var canRetry = evt.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }
}
