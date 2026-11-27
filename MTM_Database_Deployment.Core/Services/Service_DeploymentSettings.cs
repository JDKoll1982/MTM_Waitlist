using System.Text.Json;
using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Loads and saves connection settings. The JSON file is intentionally editable so a site can
/// point the tool at its own server without rebuilding.
/// </summary>
public sealed class Service_DeploymentSettings
{
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
            var settings = JsonSerializer.Deserialize(json, DeploymentSettingsJsonContext.Default.Model_ConnectionSettings)
                ?? new Model_ConnectionSettings();

            // A settings file written by an older build does not name a database that build never knew about, so the
            // shipped defaults are folded in rather than replaced: the list grows, and the saved sign-in survives.
            settings.AvailableDatabases = MergeAvailableDatabases(
                new Model_ConnectionSettings().AvailableDatabases,
                settings.AvailableDatabases);

            return settings;
        }
        catch (Exception)
        {
            return new Model_ConnectionSettings();
        }
    }

    internal static List<string> MergeAvailableDatabases(IEnumerable<string> shipped, IEnumerable<string>? saved)
    {
        var names = new List<string>();

        foreach (var name in shipped.Concat(saved ?? []))
        {
            var trimmed = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(trimmed);
            }
        }

        return names;
    }

    public void Save(Model_ConnectionSettings settings)
    {
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, DeploymentSettingsJsonContext.Default.Model_ConnectionSettings));
        }
        catch (Exception)
        {
            // Settings persistence is best effort; the in-memory values stay valid for this session.
        }
    }
}
