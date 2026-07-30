using System.Text.Json;

namespace MTM_Waitlist.Core.Helpers;

public static class Json
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static Task<string?> StringifyAsync<T>(T value)
    {
        return Task.FromResult(JsonSerializer.Serialize(value, DefaultOptions));
    }

    public static Task<T?> ToObjectAsync<T>(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(JsonSerializer.Deserialize<T>(value, DefaultOptions));
    }
}
