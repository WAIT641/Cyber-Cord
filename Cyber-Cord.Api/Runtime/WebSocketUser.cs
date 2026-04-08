using Cyber_Cord.Api.Types;
using Cyber_Cord.Api.Types.Interfaces;

namespace Cyber_Cord.Api.Runtime;

public class WebSocketUser : ICountableData<WebSocketSessionData>, IDisposable
{
    private bool _disposed;
    private bool _valid = true;

    public ReaderWriterLockSlim Mutex => new(LockRecursionPolicy.SupportsRecursion);
    public HashSet<WebSocketSessionData> Sessions { get; init; } = [];
    
    public int Count => Sessions.Count;

    public void Add(WebSocketSessionData value)
    {
        using var _ = new AutoWriteLock(Mutex);

        Sessions.Add(value);
    }

    public void Remove(WebSocketSessionData value)
    {
        using var _ = new AutoWriteLock(Mutex);

        Sessions.Remove(value);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Mutex.Dispose();

        _disposed = true;
    }

    public bool IsValid() => _valid;
    public void Invalidate()
    {
        using var _ = new AutoWriteLock(Mutex);

        _valid = false;
    }

    ~WebSocketUser()
    {
        Dispose();
    }
}
