using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MTM_Waitlist.Module_Core.Helpers;

public static class Json
{
    /// <summary>Writes a value whose generated metadata the caller supplies.</summary>
    public static Task<string> StringifyAsync<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        return Task.FromResult(JsonSerializer.Serialize(value, typeInfo));
    }

    /// <summary>Reads a value whose generated metadata the caller supplies.</summary>
    public static Task<T?> ToObjectAsync<T>(string json, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        return Task.FromResult(JsonSerializer.Deserialize(json, typeInfo));
    }

    /// <summary>Writes a value with no generated metadata.</summary>
    /// <remarks>
    /// Deliberately reflection-based: it serves a type that has no context of its own, which today means the tests.
    /// Every store that persists a value uses the <see cref="JsonTypeInfo{T}"/> overload instead.
    /// </remarks>
    public static Task<string> StringifyAsync<T>(T value)
    {
        return Task.FromResult(JsonSerializer.Serialize(value));
    }

    /// <summary>
    /// Reads a value with no generated metadata. See <see cref="StringifyAsync{T}(T)"/> for why this overload
    /// remains.
    /// </summary>
    public static Task<T?> ToObjectAsync<T>(string json)
    {
        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }
}
