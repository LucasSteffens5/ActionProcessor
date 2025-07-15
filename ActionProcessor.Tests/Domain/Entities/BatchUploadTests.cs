using ActionProcessor.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ActionProcessor.Tests.Domain.Entities;

public class BatchUploadTests
{
    [Fact]
    public void Constructor_ShouldCreateBatchWithCorrectProperties()
    {
        // Arrange
        var fileName = "test-file";
        var originalFileName = "test.csv";
        var fileSizeBytes = 1024L;
        var userEmail = "test@example.com";

        // Act
        var batch = new BatchUpload(fileName, originalFileName, fileSizeBytes, userEmail);

        // Assert
        batch.Id.Should().NotBeEmpty();
        batch.FileName.Should().Be(fileName);
        batch.OriginalFileName.Should().Be(originalFileName);
        batch.FileSizeBytes.Should().Be(fileSizeBytes);
        batch.UserEmail.Should().Be(userEmail);
        batch.Status.Should().Be(BatchStatus.Uploaded);
        batch.TotalEvents.Should().Be(0);
        batch.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        batch.StartedAt.Should().BeNull();
        batch.CompletedAt.Should().BeNull();
        batch.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Start_WhenStatusIsUploaded_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");

        // Act
        batch.Start();

        // Assert
        batch.Status.Should().Be(BatchStatus.Processing);
        batch.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Start_WhenStatusIsNotUploaded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        batch.Start(); // Set to Processing

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => batch.Start());
    }

    [Fact]
    public void Complete_WhenStatusIsProcessing_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        batch.Start();

        // Act
        batch.Complete();

        // Assert
        batch.Status.Should().Be(BatchStatus.Completed);
        batch.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_WhenStatusIsNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => batch.Complete());
    }

    [Fact]
    public void Fail_ShouldSetStatusToFailedAndSetErrorMessage()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        var errorMessage = "Something went wrong";

        // Act
        batch.Fail(errorMessage);

        // Assert
        batch.Status.Should().Be(BatchStatus.Failed);
        batch.ErrorMessage.Should().Be(errorMessage);
        batch.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetTotalEvents_ShouldUpdateTotalEventsCount()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        var totalEvents = 100;

        // Act
        batch.SetTotalEvents(totalEvents);

        // Assert
        batch.TotalEvents.Should().Be(totalEvents);
    }

    [Fact]
    public void GetProgress_WithNoEvents_ShouldReturnZeroProgress()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        batch.SetTotalEvents(10);

        // Act
        var progress = batch.GetProgress();

        // Assert
        progress.TotalEvents.Should().Be(10);
        progress.ProcessedEvents.Should().Be(0);
        progress.SuccessfulEvents.Should().Be(0);
        progress.FailedEvents.Should().Be(0);
        progress.PercentageComplete.Should().Be(0);
    }

    [Fact]
    public void GetProgress_WithMixedEvents_ShouldCalculateCorrectPercentage()
    {
        // Arrange
        var batch = new BatchUpload("test", "test.csv", 1024, "test@example.com");
        batch.SetTotalEvents(4);

        // Add some mock events through reflection for testing
        var eventsField = typeof(BatchUpload).GetField("<Events>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var events = new List<ProcessingEvent>
        {
            CreateTestEvent(batch.Id, EventStatus.Completed),
            CreateTestEvent(batch.Id, EventStatus.Completed),
            CreateTestEvent(batch.Id, EventStatus.Failed),
            CreateTestEvent(batch.Id, EventStatus.Pending)
        };
        eventsField?.SetValue(batch, events);

        // Act
        var progress = batch.GetProgress();

        // Assert
        progress.TotalEvents.Should().Be(4);
        progress.ProcessedEvents.Should().Be(3); // 2 completed + 1 failed
        progress.SuccessfulEvents.Should().Be(2);
        progress.FailedEvents.Should().Be(1);
        progress.PercentageComplete.Should().Be(75m); // 3/4 * 100
    }

    private static ProcessingEvent CreateTestEvent(Guid batchId, EventStatus status)
    {
        var evt = new ProcessingEvent(batchId, "123456789", "client1", "SAMPLE_ACTION");

        // Use reflection to set status for testing
        var statusField = typeof(ProcessingEvent).GetField("<Status>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        statusField?.SetValue(evt, status);

        return evt;
    }
}
