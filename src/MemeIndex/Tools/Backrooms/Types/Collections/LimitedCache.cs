using System.Diagnostics.CodeAnalysis;

namespace MemeIndex.Tools.Backrooms.Types.Collections;

/// Cache of a limited size.
/// When it becomes full and a new item is added, it replaces the oldest one.
public class LimitedCache<TKey, TValue> (int limit)
    where TKey : notnull
{
    private readonly LimitedQueue<TKey>        _keys = new(limit);
    private readonly Dictionary  <TKey, TValue> _map = new(limit);

    public int Count => _keys.Count;

    public void Add
        (TKey id, TValue value, out TValue? evicted)
    {
        evicted = default;

        var key  = _keys.Enqueue(id);
        if (key != null)
        {
            _map.Remove(key, out evicted);
        }

        _map.TryAdd(id, value);
    }

    public TValue? GetValueOrDefault
        (TKey id)
    {
        return _map.GetValueOrDefault(id);
    }

    public bool Contains
        (TKey id, [MaybeNullWhen(false)] out TValue value)
    {
        return _map.TryGetValue(id, out value);
    }

    public bool Contains_No
        (TKey id, [MaybeNullWhen(true)] out TValue value)
    {
        return Contains(id, out value).Janai();
    }

    public void Clear()
    {
        _keys.Clear();
        _map .Clear();
    }
}