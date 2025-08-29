using System.Collections.Concurrent;
using FlyingKit.Core.Abstractions;

namespace FlyingKit.Core.Services;

public class JobHandlersStorage
{
    private ConcurrentDictionary<string,ITickJob> _handlers = new();
    
    public void Add(string jobType, ITickJob handler)
    {
        _handlers.TryAdd(jobType, handler);
    }
    
    public ITickJob Get(string jobType)
    {
        if (_handlers.TryGetValue(jobType, out var handlerInstance))
        {
            return handlerInstance;
        }

        return null;
    }
}