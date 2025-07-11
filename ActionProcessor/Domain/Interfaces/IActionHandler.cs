using ActionProcessor.Domain.ValueObjects;

namespace ActionProcessor.Domain.Interfaces;

public interface IActionHandler
{
    string ActionType { get; }
    Task<ActionResult> ExecuteAsync(EventData eventData, CancellationToken cancellationToken = default);
}

public interface IActionHandlerFactory
{
    IActionHandler? GetHandler(string actionType);
    IEnumerable<string> GetSupportedActionTypes();
}
