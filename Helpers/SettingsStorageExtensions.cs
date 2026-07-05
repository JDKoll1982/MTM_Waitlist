using MTM_Waitlist.Core.Helpers;

using Windows.Storage;
using Windows.Storage.Streams;

namespace MTM_Waitlist.Helpers;

// Use these extension methods to store and retrieve local and roaming app data
// More details regarding storing and retrieving app data at https://docs.microsoft.com/windows/apps/design/app-settings/store-and-retrieve-app-data
public static class SettingsStorageExtensions
{
    private const string FileExtension = ".json";

    public static bool IsRoamingStorageAvailable(this ApplicationData appData)
    {
        return appData.RoamingStorageQuota == 0;
    }

    public static async Task SaveAsync<T>(this StorageFolder folder, string name, T content)
    {
        // Edge Case 1: Check for invalid or null arguments
        if (folder is null) throw new ArgumentNullException(nameof(folder));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("File name cannot be empty.", nameof(name));

        // Edge Case 2: Handle implicit null values safely
        if (content is null)
        {
            // Option A: If content is null, delete the file if it represents a "reset" state
            var fileName = GetFileName(name);
            var existingFile = await folder.TryGetItemAsync(fileName) as StorageFile;
            if (existingFile is not null)
            {
                await existingFile.DeleteAsync();
            }
            return;
        }

        // Edge Case 3: Guard against serialization failures or corrupted empty outputs
        var fileContent = await Json.StringifyAsync(content);
        if (string.IsNullOrWhiteSpace(fileContent) || fileContent == "null")
        {
            return;
        }

        // Edge Case 4: Safely overwrite files without partial data corruption from a crash
        var file = await folder.CreateFileAsync(GetFileName(name), CreationCollisionOption.ReplaceExisting);

        try
        {
            await FileIO.WriteTextAsync(file, fileContent);
        }
        catch (System.IO.FileLoadException)
        {
            // Edge Case 5: Handle potential concurrent UI/OS file locking anomalies
            await Task.Delay(50);
            await FileIO.WriteTextAsync(file, fileContent);
        }
    }

    public static async Task<T?> ReadAsync<T>(this StorageFolder folder, string name)
    {
        if (!File.Exists(Path.Combine(folder.Path, GetFileName(name))))
        {
            return default;
        }

        var file = await folder.GetFileAsync($"{name}.json");
        var fileContent = await FileIO.ReadTextAsync(file);

        return await Json.ToObjectAsync<T>(fileContent);
    }

    public static async Task SaveAsync<T>(this ApplicationDataContainer settings, string key, T value)
    {
        // Edge Case 1: Guard against null structural arguments
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be empty.", nameof(key));

        // Edge Case 2: Handle implicit or structural null variables
        if (value is null)
        {
            // Safely remove the key from Windows storage registry rather than setting it to "null"
            if (settings.Values.ContainsKey(key))
            {
                settings.Values.Remove(key);
            }
            return;
        }

        // Edge Case 3: Guard against empty or corrupt serialization strings
        var serializedValue = await Json.StringifyAsync(value);
        if (string.IsNullOrWhiteSpace(serializedValue) || serializedValue == "null")
        {
            return;
        }

        // Save the verified non-null JSON payload
        settings.SaveString(key, serializedValue);
    }

    public static void SaveString(this ApplicationDataContainer settings, string key, string value)
    {
        settings.Values[key] = value;
    }

    public static async Task<T?> ReadAsync<T>(this ApplicationDataContainer settings, string key)
    {
        object? obj;

        if (settings.Values.TryGetValue(key, out obj))
        {
            return await Json.ToObjectAsync<T>((string)obj);
        }

        return default;
    }

    public static async Task<StorageFile> SaveFileAsync(this StorageFolder folder, byte[] content, string fileName, CreationCollisionOption options = CreationCollisionOption.ReplaceExisting)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name is null or empty. Specify a valid file name", nameof(fileName));
        }

        var storageFile = await folder.CreateFileAsync(fileName, options);
        await FileIO.WriteBytesAsync(storageFile, content);
        return storageFile;
    }

    public static async Task<byte[]?> ReadFileAsync(this StorageFolder folder, string fileName)
    {
        var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

        if ((item != null) && item.IsOfType(StorageItemTypes.File))
        {
            var storageFile = await folder.GetFileAsync(fileName);
            var content = await storageFile.ReadBytesAsync();
            return content;
        }

        return null;
    }

    public static async Task<byte[]?> ReadBytesAsync(this StorageFile file)
    {
        if (file != null)
        {
            using IRandomAccessStream stream = await file.OpenReadAsync();
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);
            return bytes;
        }

        return null;
    }

    private static string GetFileName(string name)
    {
        return string.Concat(name, FileExtension);
    }
}
