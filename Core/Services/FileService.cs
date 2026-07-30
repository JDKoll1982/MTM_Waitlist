using System.Text.Json;

using MTM_Waitlist.Core.Contracts.Services;

namespace MTM_Waitlist.Core.Services;

public class FileService : IFileService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public T? Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);

        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var path = Path.Combine(folderPath, fileName);
        var json = JsonSerializer.Serialize(content, SerializerOptions);
        File.WriteAllText(path, json);
    }
}
