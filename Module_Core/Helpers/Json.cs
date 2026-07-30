using System.Text.Json;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class Json
{
    public static Task<string> StringifyAsync<T>(T value)
    {
        return Task.FromResult(JsonSerializer.Serialize(value));
    }

    public static Task<T?> ToObjectAsync<T>(string json)
    {
        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }
}
