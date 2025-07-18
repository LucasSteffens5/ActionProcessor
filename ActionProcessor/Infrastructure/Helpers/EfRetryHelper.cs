using Polly;
using Microsoft.EntityFrameworkCore;

namespace ActionProcessor.Infrastructure.Helpers;

public static class EfRetryHelper
{
    public static async Task<bool> RetryOnConcurrencyAsync(
        Func<Task> operation,
        Func<Task> onRetry,
        int maxRetries = 3,
        int delayMs = 100)
    {
        var policy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromMilliseconds(delayMs * attempt),
                async (ex, ts, attempt, ctx) => await onRetry());

        try
        {
            await policy.ExecuteAsync(operation);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
