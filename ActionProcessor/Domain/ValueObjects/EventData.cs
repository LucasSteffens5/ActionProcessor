using System.Text.Json;

namespace ActionProcessor.Domain.ValueObjects;

public record EventData(
    string Document,
    string ClientIdentifier,
    string ActionType,
    Dictionary<string, object> SideEffects
)
{
    public static EventData Parse(string csvLine)
    {
        var parts = csvLine.Split(',', StringSplitOptions.TrimEntries);
        
        if (parts.Length < 3)
            throw new ArgumentException("Invalid CSV line format. Expected at least: Document,ClientIdentifier,ActionType");
        
        var document = parts[0];
        var clientIdentifier = parts[1];
        var actionType = parts[2];
        
        var sideEffects = new Dictionary<string, object>();
        
        // Parse additional fields as side effects
        for (int i = 3; i < parts.Length; i += 2)
        {
            if (i + 1 < parts.Length)
            {
                var key = parts[i];
                var value = parts[i + 1];
                sideEffects[key] = value;
            }
        }
        
        return new EventData(document, clientIdentifier, actionType, sideEffects);
    }
    
    public string SerializeSideEffects()
    {
        return JsonSerializer.Serialize(SideEffects);
    }
    
    public static Dictionary<string, object> DeserializeSideEffects(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}

public record ActionResult(
    bool IsSuccess,
    string? ResponseData = null,
    string? ErrorMessage = null
)
{
    public static ActionResult Success(string? responseData = null) =>
        new(true, responseData);
    
    public static ActionResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}
