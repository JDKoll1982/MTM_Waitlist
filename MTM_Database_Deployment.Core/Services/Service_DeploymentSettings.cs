using System.Text.Json;
using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Loads and saves connection settings. The JSON file is intentionally editable so a site can
/// point the tool at its own server without rebuilding.
/// </summary>
public sealed class Service_DeploymentSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsPath { get; }

    public string SqlRoot { get; }

    public Service_DeploymentSettings()
    {
        SettingsPath = Path.Combine(AppContext.BaseDirectory, "deployment.settings.json");
        SqlRoot = ResolveSqlRoot();
    }

    private static string ResolveSqlRoot()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "Database");
        if (Directory.Exists(Path.Combine(local, "Tables")))
        {
            return local;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Database");
            if (Directory.Exists(Path.Combine(candidate, "Tables")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return local;
    }

    public Model_ConnectionSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaults = new Model_ConnectionSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<Model_ConnectionSettings>(json, SerializerOptions);
            return settings ?? new Model_ConnectionSettings();
        }
        catch (Exception)
        {
            return new Model_ConnectionSettings();
        }
    }

    public void Save(Model_ConnectionSettings settings)
    {
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (Exception)
        {
            // Settings persistence is best effort; the in-memory values stay valid for this session.
        }
    }
}
