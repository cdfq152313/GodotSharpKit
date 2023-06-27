namespace GdExtension;

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
}
