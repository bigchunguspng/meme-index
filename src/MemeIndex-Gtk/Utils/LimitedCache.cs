namespace MemeIndex_Gtk.Utils;

public class LimitedCache<T>
{
    private readonly int _maxSize;
    private readonly Queue<string> _keys;
    private readonly Dictionary<string, T> _dictionary;

    public LimitedCache(int maxSize)
    {
        _maxSize = maxSize;
        _keys = new Queue<string>(_maxSize);
        _dictionary = new Dictionary<string, T>(_maxSize);
    }

    public void Add(string key, T value)
    {
        if (_keys.Count >= _maxSize)
        {
            var first = _keys.Dequeue();
            _dictionary.Remove(first);
        }

        _keys.Enqueue(key);
        _dictionary.TryAdd(key, value);
    }

    public T? TryGetValue(string key)
    {
        _dictionary.TryGetValue(key, out var result);
        return result;
    }
}