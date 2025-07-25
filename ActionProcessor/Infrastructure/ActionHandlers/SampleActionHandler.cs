using ActionProcessor.Domain.Interfaces;
using ActionProcessor.Domain.ValueObjects;
using Polly;
using System.Text.Json;
using ActionProcessor.Infrastructure.ActionHandlers.SideEffects;

namespace ActionProcessor.Infrastructure.ActionHandlers;

public class SampleActionHandler(HttpClient httpClient, ILogger<SampleActionHandler> logger) : IActionHandler
{
    public string ActionType => "SAMPLE_ACTION";

    public async Task<ActionResult> ExecuteAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Executing SAMPLE_ACTION for document: {Document}, client: {ClientIdentifier}",
                eventData.Document, eventData.ClientIdentifier);
            
            var sideEffects = SampleSideEffects.FromJson(eventData.SideEffectsJson);
            
            if (!sideEffects.IsValid())
            {
                logger.LogWarning("Invalid or missing SideEffects for document: {Document}", eventData.Document);
            }

            // Example external API call with retry policy
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        logger.LogWarning("Retry {RetryCount} for document {Document} after {Delay}ms",
                            retryCount, eventData.Document, timespan.TotalMilliseconds);
                    });

            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(100, cancellationToken); // Simulate network delay

                // Simulate some failures for testing
                var random = new Random();
                if (random.Next(1, 100) <= 5) // 5% failure rate
                {
                    throw new HttpRequestException("Simulated external API failure");
                }

                return new
                {
                    Success = true,
                    TransactionId = Guid.NewGuid().ToString(),
                    ProcessedAt = DateTime.UtcNow,
                    Message = "Sample action completed successfully"
                };
            });

            var responseJson = JsonSerializer.Serialize(response);
            logger.LogInformation("SAMPLE_ACTION completed successfully for document: {Document}", eventData.Document);

            return ActionResult.Success(responseJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SAMPLE_ACTION failed for document: {Document}", eventData.Document);
            return ActionResult.Failure($"External API call failed: {ex.Message}");
        }
    }
}
