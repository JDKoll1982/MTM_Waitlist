namespace MTM_Database_Deployment.Models;

/// <summary>
/// Connection settings for the deployment target. Values are loaded from deployment.settings.json
/// and never contain secrets that are committed to source control.
/// </summary>
public sealed class Model_ConnectionSettings
{
    public const string LiveDatabase = "mtm_waitlist";
    public const string TestDatabase = "mtm_waitlist_test";

    public string PreferredServer { get; set; } = "172.16.1.104";
    public string FallbackServer { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = LiveDatabase;
    public string User { get; set; } = "root";

    /// <summary>
    /// Empty, because this file is committed. A site fills it on the machine that deploys, or the operator types it
    /// into the sign-in flyout, which writes it back to that PC only.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Databases the operator may pick from. A site can add its own names here without a rebuild; anything the
    /// server holds under the same prefix is added to this list after Connect, so a copy made with the tool does not
    /// have to be typed in here first.
    /// </summary>
    public List<string> AvailableDatabases { get; set; } = [LiveDatabase, TestDatabase, "mtm_mock", "mtm_mock_test"];

    /// <summary>
    /// The selectable databases, guaranteed to contain <see cref="Database"/> so a name that is not in
    /// the list is never silently swapped for another one.
    /// </summary>
    public IReadOnlyList<string> ResolveAvailableDatabases()
    {
        var names = new List<string>();
        foreach (var candidate in AvailableDatabases ?? [])
        {
            var name = candidate?.Trim();
            if (!string.IsNullOrWhiteSpace(name)
                && !names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        var current = Database?.Trim();
        if (!string.IsNullOrWhiteSpace(current)
            && !names.Contains(current, StringComparer.OrdinalIgnoreCase))
        {
            names.Insert(0, current);
        }

        if (names.Count == 0)
        {
            names.Add(LiveDatabase);
        }

        return names;
    }

    public Model_ConnectionSettings Clone() => new()
    {
        PreferredServer = PreferredServer,
        FallbackServer = FallbackServer,
        Port = Port,
        Database = Database,
        User = User,
        Password = Password,
        ConnectTimeoutSeconds = ConnectTimeoutSeconds,
        AvailableDatabases = [.. AvailableDatabases ?? []]
    };
}
