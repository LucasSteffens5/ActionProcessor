namespace ActionProcessor.Domain.ValueObjects;

public record EventData(
    string Document,
    string ClientIdentifier,
    string ActionType,
    string? SideEffectsJson = null
)
{
    public static EventData Parse(string csvLine)
    {
        var parts = csvLine.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length < 3)
            throw new ArgumentException(
                "Invalid CSV line format. Expected at least: Document,ClientIdentifier,ActionType");

        var document = parts[0];
        var clientIdentifier = parts[1];
        var actionType = parts[2];

        return new EventData(document, clientIdentifier, actionType);
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