using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MemeIndex.Tools.Backrooms.Extensions;

public static class Extensions_Generic
{
    public static T Fluent<T>
        (this T obj, Action action)
    {
        action();
        return obj;
    }

    public static bool TryGetValue_Failed<TKey,TValue>
    (
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        [MaybeNullWhen(true)] out TValue value
    ) where TKey : notnull
    {
        return dictionary.TryGetValue(key, out value).Failed();
    }

    public static Span<T> Slice<T>(this T[] array, int length)
    {
        return array.AsSpan(0, length);
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var element in source) action(element);
    }

    public static string? ToLower<T>(this T obj)
    {
        return obj?.ToString()?.ToLower();
    }

    public static T GetRandomMemeber<T>() where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        return (T)values.GetValue(Random.Shared.Next(values.Length))!;
    }

    public static T PickAny<T>(this ICollection<T> collection)
    {
        return collection.ElementAt(Random.Shared.Next(collection.Count));
    }

    public static int FindIndex<T>(this T[] array, Predicate<T> match)
    {
        var l = array.Length;
        for (var i = 0; i < l; i++)
        {
            if (match(array[i])) return i;
        }

        return -1;
    }

    public static int FindLastIndex<T>(this T[] array, Predicate<T> match)
    {
        for (var i = array.Length - 1; i >= 0; i--)
        {
            if (match(array[i])) return i;
        }

        return -1;
    }

    public static async Task<T?> OrDefault_OnException<T>(this Task<T> getThing)
    {
        try
        {
            return await getThing;
        }
        catch
        {
            return default;
        }
    }

    /// Creates an array of given length, populated by given factory.
    public static T[] Times<T>(this int length, Func<T> factory)
    {
        var array = new T[length];
        for (var i = 0; i < length; i++)
            array[i] = factory();
        return array;
    }

    /// Creates an array of given length, populated by given factory.
    /// Factory accepts item index.
    public static T[] Times<T>(this int length, Func<int, T> factory)
    {
        var array = new T[length];
        for (var i = 0; i < length; i++)
            array[i] = factory(i);
        return array;
    }

    /// For loop.
    public static void LoopTill<T>(this T start, T limit, T step, Action<T> action) where T : INumber<T>
    {
        for (var i = start; i < limit; i += step) action(i);
    }

    // Meme-Index

    /// Executes an action based on a condition value.
    public static void Execute(this bool condition, Action onTrue, Action onFalse)
    {
        (condition ? onTrue : onFalse)();
    }

    /// Just a wrapper for a ternary operator (useful with delegates). 
    public static T Switch<T>(this bool condition, T onTrue, T onFalse)
    {
        return condition ? onTrue : onFalse;
    }

    public static void ForEachTry<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            try
            {
                action(item);
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
    }

    public static void ForEachTry<T>(this Span<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            try
            {
                action(item);
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
    }
}