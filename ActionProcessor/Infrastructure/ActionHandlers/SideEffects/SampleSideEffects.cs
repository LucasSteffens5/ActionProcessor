using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActionProcessor.Infrastructure.ActionHandlers.SideEffects;

public record SampleSideEffects(
    [property: JsonPropertyName("edipi")] long? Edipi = null,
    [property: JsonPropertyName("firstName")] string? FirstName = null,
    [property: JsonPropertyName("lastName")] string? LastName = null,
    [property: JsonPropertyName("department")] string? Department = null,
    [property: JsonPropertyName("clearanceLevel")] string? ClearanceLevel = null,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("requestDate")] DateTime? RequestDate = null
)
{
    public static SampleSideEffects FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SampleSideEffects();

        try
        {
            return JsonSerializer.Deserialize<SampleSideEffects>(json) ?? new SampleSideEffects();
        }
        catch (JsonException)
        {
            return new SampleSideEffects();
        }
    }

    public bool IsValid()
        => !string.IsNullOrWhiteSpace(FirstName) ||
           !string.IsNullOrWhiteSpace(LastName) ||
           Edipi.HasValue;


    public string GetFullName()
        => $"{FirstName} {LastName}".Trim();
}