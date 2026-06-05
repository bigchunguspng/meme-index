using System.Diagnostics.CodeAnalysis;

namespace MemeIndex.Tools.Backrooms.Types.Collections;

[Obsolete("Unused! Otherwise remove this attribute.")]
public class TemporaryCache<T>(TimeSpan retention)
{
    private T? Value;
    private DateTime EOL;

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = Value;
        return value != null && EOL > DateTime.Now;
    }

    public bool TryGetValue_Failed([MaybeNullWhen(true)] out T value)
    {
        return TryGetValue(out value).Failed();
    }

    public void Set(T value)
    {
        Value = value;
        EOL = DateTime.Now + retention;
    }
}