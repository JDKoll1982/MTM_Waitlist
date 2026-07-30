namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IFileService
{
    Task<T?> Read<T>(string directoryPath, string fileName);
    Task Save<T>(string directoryPath, string fileName, T value);
    Task Delete(string directoryPath, string fileName);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
}
