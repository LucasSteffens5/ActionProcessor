using ActionProcessor.Domain.Interfaces;

namespace ActionProcessor.Infrastructure.ActionHandlers;

public class ActionHandlerFactory : IActionHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _handlers;
    
    public ActionHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlers = new Dictionary<string, Type>();
        
        // Register action handlers
        RegisterHandler<SampleActionHandler>();
        // Add more handlers here as they are implemented
    }
    
    private void RegisterHandler<T>() where T : class, IActionHandler
    {
        var handler = _serviceProvider.GetService<T>();
        if (handler != null)
        {
            _handlers[handler.ActionType] = typeof(T);
        }
    }
    
    public IActionHandler? GetHandler(string actionType)
    {
        if (_handlers.TryGetValue(actionType, out var handlerType))
        {
            return _serviceProvider.GetService(handlerType) as IActionHandler;
        }
        
        return null;
    }
    
    public IEnumerable<string> GetSupportedActionTypes()
    {
        return _handlers.Keys;
    }
}
