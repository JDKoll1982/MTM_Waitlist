using System.Text;

namespace MTM_Waitlist.Module_Setup.Services;

internal static class SetupSqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "InforVisual";
    private const string ModuleFolderName = "Queues";
    private const string ScriptModuleFolderName = "Module_Setup";
    private const string ScriptQueryFolderName = "Queries";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
                ScriptFolderName,
                QueryFolderName,
                ModuleFolderName,
                ScriptModuleFolderName,
                ScriptQueryFolderName,
            normalizedName);
    }
}

internal static class SetupReceivingMySqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "MTMReceivingApp";
    private const string DatabaseFolderName = "Queues";
    private const string ModuleFolderName = "Module_Setup";
    private const string ScriptQueryFolderName = "Queues";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            DatabaseFolderName,
            ModuleFolderName,
            ScriptQueryFolderName,
            normalizedName);
    }
}

internal static class SetupWaitlistMySqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "StoredProcedures";
    private const string ProcedureFolderName = "sp_setup_save_setup";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            ProcedureFolderName,
            normalizedName);
    }
}

internal static class SetupReceivingStoredProcedureScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "MTMReceivingApp";
    private const string ProcedureFolderName = "StoredProcedures";

    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        var scriptPath = GetScriptPath(scriptName);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static string GetScriptPath(string scriptName)
    {
        var normalizedName = scriptName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            ? scriptName
            : $"{scriptName}.sql";

        return Path.Combine(
            AppContext.BaseDirectory,
            ScriptFolderName,
            QueryFolderName,
            ProcedureFolderName,
            normalizedName);
    }
}
