using System.Text.Json;

namespace GdExtension;

public class ConfigUtil
{
    private static JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static Config Read(string value)
    {
        return JsonSerializer.Deserialize<Config>(value, _options)!;
    }
}
