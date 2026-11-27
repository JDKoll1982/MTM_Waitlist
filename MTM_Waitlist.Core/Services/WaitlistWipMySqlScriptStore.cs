using System.Text;

namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Resolves checked-in MTM WIP Application MySQL queue scripts for the Waitlist module from the app
/// output (<c>Database/MTMWipApp/Queues/Module_Waitlist/Queues</c>).
/// </summary>
/// <remarks>
/// The reviewed set below is the only set of names this store will resolve. A name it does not hold is
/// refused here rather than turned into a path, so the reviewed-script route cannot be widened by passing a
/// name, a relative path, or statement text to it.
/// </remarks>
internal static class WaitlistWipMySqlScriptStore
{
    private const string ScriptFolderName = "Database";
    private const string QueryFolderName = "MTMWipApp";
    private const string ModuleFolderName = "Queues";
    private const string ScriptModuleFolderName = "Module_Waitlist";
    private const string ScriptQueryFolderName = "Queues";

    /// <summary>The checked-in queue scripts this module is allowed to run.</summary>
    private static readonly HashSet<string> s_reviewedScriptNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "GetWipFloorQuantities",
        "GetWipInventoryPartNumbers",
        "GetWipInventoryLocations",
        "GetWipInventoryPartMatches",
        "GetWipPartOperations",
        "GetWipOperationQuantity",
    };

    /// <summary>Whether a name is one of the reviewed queue scripts.</summary>
    /// <param name="scriptName">The name to check.</param>
    /// <returns><c>true</c> when the store holds that script.</returns>
    public static bool IsReviewed(string scriptName) =>
        !string.IsNullOrWhiteSpace(scriptName) && s_reviewedScriptNames.Contains(scriptName.Trim());

    /// <summary>
    /// The text of a reviewed queue script, or an empty string when the name is not reviewed or the script is
    /// missing from this installation.
    /// </summary>
    /// <param name="scriptName">The script's name, without the <c>.sql</c> suffix.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    public static async Task<string> LoadAsync(string scriptName, CancellationToken cancellationToken = default)
    {
        if (!IsReviewed(scriptName))
        {
            return string.Empty;
        }

        var scriptPath = GetScriptPath(scriptName.Trim());
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
