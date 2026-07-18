using System.Text;
using MTM_Waitlist.Core.Contracts.Services;
using Newtonsoft.Json;

namespace MTM_Waitlist.Core.Services;

public class FileService : IFileService
{
    public T? Read<T>(string folderPath, string fileName)
    {
        // Edge Case 1: Validate parameters to prevent empty path computation crashes
        if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return default;
        }

        var path = Path.Combine(folderPath, fileName);

        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            var json = File.ReadAllText(path);

            // Edge Case 2: Guard against empty files or literal "null" text values
            if (string.IsNullOrWhiteSpace(json) || json.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception ex) when (ex is IOException or JsonReaderException)
        {
            // Edge Case 3: Handle active file locks or malformed JSON formats gracefully
            return default;
        }
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        // Edge Case 1 (Continued): Quick parameter validation
        if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Folder path and file name must be provided.");
        }

        // Edge Case 4: If content is null, interpret as a command to clear the file rather than corrupting it
        if (content is null)
        {
            Delete(folderPath, fileName);
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        try
        {
            var fileContent = JsonConvert.SerializeObject(content);

            // Edge Case 2 (Continued): Ensure we didn't serialize to an explicit text representation of "null"
            if (string.IsNullOrWhiteSpace(fileContent) || fileContent == "null")
            {
                return;
            }

            var fullPath = Path.Combine(folderPath, fileName);
            File.WriteAllText(fullPath, fileContent, Encoding.UTF8);
        }
        catch (IOException)
        {
            // Edge Case 5: Retry loop or safe pass-through if the OS temporarily blocks the write handle
            try
            {
                Thread.Sleep(50); // Small synchronous delay buffer for desktop file locks
                var fullPath = Path.Combine(folderPath, fileName);
                var fileContent = JsonConvert.SerializeObject(content);
                File.WriteAllText(fullPath, fileContent, Encoding.UTF8);
            }
            catch
            {
                // Suppress or log structural background thread concurrency collisions
            }
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        // Edge Case 6: Clean verification checking for structural directory validity before deletion
        if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        try
        {
            var path = Path.Combine(folderPath, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Fail silently if file is open or locked during a destructive routine call
        }
    }
}
