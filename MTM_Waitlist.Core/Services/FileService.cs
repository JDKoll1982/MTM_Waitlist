namespace MTM_Waitlist.Module_Core.Services;

using System.Text.Json;
using MTM_Waitlist.Module_Core.Contracts.Services;

public sealed class FileService : IFileService
{
    public async Task<T?> Read<T>(string directoryPath, string fileName)
    {
        var fullPath = Path.Combine(directoryPath, fileName);
        if (!File.Exists(fullPath))
        {
            return default;
        }

        var contents = await File.ReadAllTextAsync(fullPath);
        return JsonSerializer.Deserialize<T>(contents);
    }

    public async Task Save<T>(string directoryPath, string fileName, T value)
    {
        Directory.CreateDirectory(directoryPath);
        var fullPath = Path.Combine(directoryPath, fileName);
        var contents = JsonSerializer.Serialize(value);
        await File.WriteAllTextAsync(fullPath, contents);
    }

    public Task Delete(string directoryPath, string fileName)
    {
        var fullPath = Path.Combine(directoryPath, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}
