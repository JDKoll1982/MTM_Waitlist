using Newtonsoft.Json;

namespace MTM_Waitlist.Core.Helpers;

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        // Edge Case 1: Guard against null, empty, or whitespace data configurations
        if (string.IsNullOrWhiteSpace(value))
        {
            return default; // Returns null for classes, or default value for value types (like 0 or false)
        }

        // Edge Case 2: Guard against a valid string representation of the word "null"
        if (value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        try
        {
            // Execute the CPU-bound parsing logic on a safe background worker thread
            return await Task.Run(() => JsonConvert.DeserializeObject<T>(value));
        }
        catch (JsonReaderException)
        {
            // Edge Case 3: Handle malformed or corrupted JSON strings gracefully without crashing
            return default;
        }
    }

    public static async Task<string> StringifyAsync(object? value)
    {
        // Edge Case 4: Handle an uninstantiated null object parameter upfront
        if (value is null)
        {
            return "{}"; // Return an empty JSON object string instead of the corrupting "null" literal string
        }

        try
        {
            // Execute serialization safely on a background thread
            var result = await Task.Run(() => JsonConvert.SerializeObject(value));

            // Edge Case 5: Final double-check to block unintended "null" string creations
            return string.IsNullOrWhiteSpace(result) || result == "null" ? "{}" : result;
        }
        catch (JsonSerializationException)
        {
            // Edge Case 6: Fallback placeholder string if a complex circular-reference property fails to serialize
            return "{}";
        }
    }
}