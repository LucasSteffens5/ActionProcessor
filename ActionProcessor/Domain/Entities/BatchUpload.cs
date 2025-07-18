using System.ComponentModel.DataAnnotations;

namespace ActionProcessor.Domain.Entities;

public class BatchUpload
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public int TotalEvents { get; private set; }
    public BatchStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    [Timestamp]
    public byte[] RowVersion { get; private set; } = null!;
    
    public ICollection<ProcessingEvent> Events { get; private set; } = new List<ProcessingEvent>();

    private BatchUpload()
    {
    }

    public BatchUpload(string fileName, string originalFileName, long fileSizeBytes, string userEmail)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        OriginalFileName = originalFileName;
        FileSizeBytes = fileSizeBytes;
        UserEmail = userEmail;
        Status = BatchStatus.Uploaded;
        CreatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status != BatchStatus.Uploaded)
            throw new InvalidOperationException($"Cannot start batch in {Status} status");

        Status = BatchStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != BatchStatus.Processing)
            throw new InvalidOperationException($"Cannot complete batch in {Status} status");

        Status = BatchStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = BatchStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public bool StartProcessing()
    {
        if (Status != BatchStatus.Uploaded) return false;
        Status = BatchStatus.Processing;
        StartedAt = DateTime.UtcNow;
        return true;
    }

    public BatchStatus DetermineCompletionStatus()
    {
        if (Events.Count == 0) return BatchStatus.Failed;
        
        var allFailed = Events.All(e => e.Status == EventStatus.Failed);
        if (allFailed) return BatchStatus.Failed;
        
        var allProcessed = Events.All(e => e.Status is EventStatus.Completed or EventStatus.Failed);
        return allProcessed ? BatchStatus.Completed : BatchStatus.Processing;
    }

    public void SetTotalEvents(int totalEvents)
    {
        TotalEvents = totalEvents;
    }

    public bool IsActive()
        => Status is BatchStatus.Uploaded or BatchStatus.Processing;

    public bool HasPendingEvents()
        => Events.Any(e => e.Status is EventStatus.Pending or EventStatus.Processing);

    public BatchProgress GetProgress()
    {
        var processedCount = Events.Count(e => e.Status is EventStatus.Completed or EventStatus.Failed);
        var successCount = Events.Count(e => e.Status == EventStatus.Completed);
        var failedCount = Events.Count(e => e.Status == EventStatus.Failed);

        var percentage = TotalEvents > 0 ? (processedCount * 100.0m) / TotalEvents : 0;

        return new BatchProgress(
            Id,
            Status,
            TotalEvents,
            processedCount,
            successCount,
            failedCount,
            percentage,
            CreatedAt,
            StartedAt,
            CompletedAt
        );
    }
}

public enum BatchStatus
{
    Uploaded = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public record BatchProgress(
    Guid BatchId,
    BatchStatus Status,
    int TotalEvents,
    int ProcessedEvents,
    int SuccessfulEvents,
    int FailedEvents,
    decimal PercentageComplete,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);