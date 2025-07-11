namespace ActionProcessor.Domain.Entities;

public class ProcessingEvent
{
    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string Document { get; private set; } = string.Empty;
    public string ClientIdentifier { get; private set; } = string.Empty;
    public string ActionType { get; private set; } = string.Empty;
    public string SideEffectsJson { get; private set; } = "{}";
    public EventStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResponseData { get; private set; }
    
    // Navigation properties
    public BatchUpload Batch { get; private set; } = null!;
    
    private ProcessingEvent() { } // EF Constructor
    
    public ProcessingEvent(
        Guid batchId,
        string document,
        string clientIdentifier,
        string actionType,
        string sideEffectsJson = "{}")
    {
        Id = Guid.NewGuid();
        BatchId = batchId;
        Document = document;
        ClientIdentifier = clientIdentifier;
        ActionType = actionType;
        SideEffectsJson = sideEffectsJson;
        Status = EventStatus.Pending;
        RetryCount = 0;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void Start()
    {
        if (Status != EventStatus.Pending && Status != EventStatus.Failed)
            throw new InvalidOperationException($"Cannot start event in {Status} status");
            
        Status = EventStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(string? responseData = null)
    {
        if (Status != EventStatus.Processing)
            throw new InvalidOperationException($"Cannot complete event in {Status} status");
            
        Status = EventStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ResponseData = responseData;
        ErrorMessage = null;
    }
    
    public void Fail(string errorMessage)
    {
        if (Status != EventStatus.Processing)
            throw new InvalidOperationException($"Cannot fail event in {Status} status");
            
        Status = EventStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
    }
    
    public void ResetForRetry()
    {
        if (Status != EventStatus.Failed)
            throw new InvalidOperationException($"Cannot retry event in {Status} status");
            
        Status = EventStatus.Pending;
        StartedAt = null;
        CompletedAt = null;
        ErrorMessage = null;
    }
    
    public bool CanRetry(int maxRetries = 3)
    {
        return Status == EventStatus.Failed && RetryCount < maxRetries;
    }
}

public enum EventStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
