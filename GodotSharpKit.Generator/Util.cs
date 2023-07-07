using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GodotSharpKit.Generator;

public static class Util
{
    public static TV? GetValue<TK, TV>(
        this IDictionary<TK, TV> dict,
        TK key,
        TV? defaultValue = default
    )
    {
        TV? value;
        return dict.TryGetValue(key, out value) ? value : defaultValue;
    }

    public static T Get<T>(this ImmutableArray<TypedConstant> array, int index, T defaultValue)
    {
        return index < array.Length ? (T)array[index].Value! : defaultValue;
    }

    public static T? GetOrNull<T>(
        this ImmutableArray<TypedConstant> array,
        int index,
        T? defaultValue = default
    ) where T : class
    {
        return index < array.Length ? array[index].Value as T : defaultValue;
    }

    public static int GetSequenceHashCode<T>(this IList<T> sequence) where T : notnull
    {
        const int seed = 487;
        const int modifier = 31;
        unchecked
        {
            return sequence.Aggregate(
                seed,
                (current, item) => (current * modifier) + item.GetHashCode()
            );
        }
    }
}
