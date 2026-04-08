using System.Collections.Concurrent;
using Cyber_Cord.Api.Types.Interfaces;

namespace Cyber_Cord.Api.Types.Collections;

public class AutoUpdateConcurrentDictionary<TKey, TValue, TCountKey> : ConcurrentDictionary<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : ICountableData<TCountKey>
{
    public TValue PushOrAdd(TKey key, TCountKey countKey, TValue value)
    {
        return AddOrUpdate(key, _ => AddToValue(value), (_, prevValue) => {
            using var locker = new AutoWriteLock(prevValue.Mutex); // Write is used because .Add also needs WriteLock

            return prevValue.IsValid()
                ? AddToValue(prevValue)
                : AddToValue(value);
        });

        TValue AddToValue(TValue value)
        {
            value.Add(countKey);
            return value;
        }
    }

    public bool PopOrRemove(TKey key, TCountKey countKey, Action<TValue> cleanupFunction)
    {
        if (!TryGetValue(key, out var value))
        {
            return false;
        }
        
        using (var _ = new AutoWriteLock(value.Mutex))
        {
            value.Remove(countKey);

            if (value.Count != 0)
            {
                return false;
            }

            value.Invalidate();
        }

        var pair = new KeyValuePair<TKey, TValue>(key, value);

        var returnValue = TryRemove(pair);

        cleanupFunction(value);

        return returnValue;
    }
}
