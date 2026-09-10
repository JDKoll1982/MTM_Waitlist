using System.Text;
using MTM_Waitlist.Mock.Contracts;

namespace MTM_Waitlist.Mock.Services;

/// <summary>
/// Loads Infor Visual queue scripts from the application's content root.
/// </summary>
/// <remarks>
/// Replaces the retired Module_Setup <c>SetupSqlScriptStore</c> and the Module_Core
/// <c>WaitlistInforVisualSqlScriptStore</c>, which differed only in a hard-coded module folder. The
/// script path now comes from the read shape itself, so one store serves every shape and a new shape
/// needs no change here.
/// </remarks>
public sealed class InforVisualScriptStore : IInforVisualScriptStore
{
    private readonly string _contentRoot;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="contentRoot">
    /// Root the script paths are resolved against. Defaults to the running application's base
    /// directory, which is where the checked-in <c>Database/InforVisual/Queues</c> content ships.
    /// </param>
    public InforVisualScriptStore(string? contentRoot = null)
    {
        _contentRoot = string.IsNullOrWhiteSpace(contentRoot) ? AppContext.BaseDirectory : contentRoot;
    }

    /// <inheritdoc />
    public async Task<string> LoadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var scriptPath = Path.Combine(_contentRoot, normalizedRelativePath);
        if (!File.Exists(scriptPath))
        {
            return string.Empty;
        }

        await using var stream = File.OpenRead(scriptPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        cancellationToken.ThrowIfCancellationRequested();
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}
