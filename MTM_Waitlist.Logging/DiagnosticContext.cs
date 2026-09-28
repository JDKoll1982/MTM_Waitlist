using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// Gathers the context the seam writes into <c>payload_json</c>, so no call site has to remember any of it
/// (`contracts/logging-contract.md` §1.3, FR-034 to FR-036).
/// </summary>
/// <remarks>
/// <para>
/// <b>Three objects, one column.</b> <c>runtime</c> describes the application and the machine it is running on,
/// <c>ui</c> describes the surface the caller could name, and <c>database</c> describes the store operation the
/// fault came out of. They are one JSON document because the panel shows them together on the entry card.
/// </para>
/// <para>
/// <b>Gathering is defensive and never touches the dispatcher.</b> A fault raised while the queue is gone must
/// not raise a second one while it tries to describe itself, so nothing here reads a window, a page or a
/// dispatcher — only what the entry already carries and what the runtime answers on any thread.
/// <see cref="EnvironmentName"/> is not gathered at all: the machine keeps only the connection strings it needs
/// to reach the store, so there is no local configuration to read it from (FR-025).
/// </para>
/// <para>
/// <b>Nothing on the never-write list is gathered.</b> Connection strings, credentials, session tokens and key
/// material are never read here, and the store object carries the configured database name rather than any
/// connection text (`contracts/logging-contract.md` §6).
/// </para>
/// </remarks>
public static class DiagnosticContext
{
    /// <summary>
    /// Builds the entry's <c>payload_json</c>.
    /// </summary>
    /// <param name="entry">The entry the caller described; its target is the UI member that can be read safely.</param>
    /// <param name="exception">The fault, when there is one, so a store fault can carry the provider's own codes.</param>
    /// <param name="properties">The structured properties an <c>ILogger</c> call site already carries, when any.</param>
    /// <param name="databaseName">The configured database the store write goes to.</param>
    /// <param name="storedProcedureName">The procedure the store write goes through.</param>
    /// <param name="processStartedUtc">When the process began, so the entry can carry the application's uptime.</param>
    /// <returns>A JSON object; an empty object when nothing could be gathered at all.</returns>
    public static string Build(
        LogEntry entry,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties,
        string databaseName,
        string storedProcedureName,
        DateTime processStartedUtc)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var payload = new JsonObject
            {
                ["runtime"] = BuildRuntime(processStartedUtc),
                ["ui"] = BuildUi(entry),
                ["database"] = BuildDatabase(exception, databaseName, storedProcedureName),
            };

            if (properties is { Count: > 0 })
            {
                payload["properties"] = BuildProperties(properties);
            }

            return payload.ToJsonString();
        }
        catch (Exception)
        {
            return new JsonObject().ToJsonString();
        }
    }

    private static JsonObject BuildRuntime(DateTime processStartedUtc)
    {
        var assembly = Assembly.GetEntryAssembly();

        return new JsonObject
        {
            ["application"] = Read(() => assembly?.GetName().Name),
            ["version"] = Read(() => assembly?.GetName().Version?.ToString()),
            ["informationalVersion"] = Read(() => assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion),
            ["osDescription"] = Read(() => RuntimeInformation.OSDescription),
            ["osArchitecture"] = Read(() => RuntimeInformation.OSArchitecture.ToString()),
            ["processArchitecture"] = Read(() => RuntimeInformation.ProcessArchitecture.ToString()),
            ["is64BitProcess"] = Read(() => Environment.Is64BitProcess),
            ["is64BitOperatingSystem"] = Read(() => Environment.Is64BitOperatingSystem),
            ["framework"] = Read(() => RuntimeInformation.FrameworkDescription),
            ["windowsAppSdkVersion"] = Read(() => typeof(Microsoft.UI.Xaml.Application).Assembly.GetName().Version?.ToString()),
            ["isPackaged"] = Read(() => RuntimeHelper.IsMSIX),
            ["processId"] = Read(() => Environment.ProcessId),
            ["threadId"] = Read(() => Environment.CurrentManagedThreadId),
            ["uptimeSeconds"] = Read(() => (long)Math.Max(0, (DateTime.UtcNow - processStartedUtc).TotalSeconds)),
            ["culture"] = Read(() => CultureInfo.CurrentCulture.Name),
            ["uiCulture"] = Read(() => CultureInfo.CurrentUICulture.Name),
        };
    }

    private static JsonObject BuildUi(LogEntry entry)
    {
        // Only what the entry carries: reading the live window, page or control would touch the dispatcher, which
        // is exactly what a fault raised during shutdown must not do. A caller that knows the control names it
        // through the entry's target.
        return new JsonObject
        {
            ["window"] = null,
            ["screen"] = null,
            ["control"] = Read(() => Blank(entry.Target) ? null : entry.Target),
            ["controlType"] = null,
            ["event"] = null,
            ["command"] = null,
            ["isUiThread"] = null,
        };
    }

    private static JsonObject BuildDatabase(Exception? exception, string databaseName, string storedProcedureName)
    {
        return new JsonObject
        {
            ["provider"] = Read(() => ProviderValue(exception, "provider")),
            ["providerVersion"] = Read(() => ProviderValue(exception, "providerVersion")),
            ["errorCode"] = Read(() => ProviderValue(exception, "ErrorCode")),
            ["sqlState"] = Read(() => ProviderValue(exception, "SqlState")),
            ["number"] = Read(() => ProviderValue(exception, "Number")),
            ["database"] = databaseName,
            // The store's alias is left null on purpose. The only local configuration that names the server is
            // the connection string, and writing any part of that text is forbidden (contract §6): a server
            // alias is a convenience, a connection string is a credential.
            ["operation"] = "insert",
            ["storedProcedure"] = storedProcedureName,
            ["retryAttempt"] = null,
            ["transactionActive"] = null,
            ["transactionIsolationLevel"] = null,
            ["connectionState"] = null,
            ["commandTimeoutSeconds"] = null,
            ["connectionTimeoutSeconds"] = null,
            ["affectedRows"] = null,
            ["elapsedMilliseconds"] = null,
            ["statementFingerprint"] = null,
        };
    }

    private static JsonObject BuildProperties(IReadOnlyDictionary<string, object?> properties)
    {
        var projected = new JsonObject();
        var removed = 0;

        foreach (var (name, value) in properties)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            // A structured property is a name a call site chose, so it is as capable of naming a credential as
            // an exception's data is. §6 forbids the value, and the name is recorded as a count instead of as
            // itself: whether a call site grouped its properties by "password" is a fact about the call site.
            if (ExceptionDetailSerializer.IsNeverWritten(name))
            {
                removed++;
                continue;
            }

            projected[name] = ReadValue(value);
        }

        if (removed > 0)
        {
            projected["valuesRemoved"] = removed;
        }

        return projected;
    }

    private static JsonNode? ReadValue(object? value) => value switch
    {
        null => null,
        string text => text,
        bool flag => JsonValue.Create(flag),
        sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
            => JsonValue.Create(value.ToString()),
        _ => $"<{value.GetType().FullName}>",
    };

    /// <summary>
    /// Reads a property from a provider exception by name, so the logging module stays independent of whichever
    /// MySQL provider the application happens to reference and a provider that does not expose a member is simply
    /// not described (FR-036).
    /// </summary>
    private static object? ProviderValue(Exception? exception, string memberName)
    {
        if (exception is null)
        {
            return null;
        }

        try
        {
            var type = exception.GetType();

            if (memberName == "provider")
            {
                return type.Namespace ?? type.Name;
            }

            if (memberName == "providerVersion")
            {
                return type.Assembly.GetName().Version?.ToString();
            }

            var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(exception)?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Runs a read that may throw — a runtime probe on an unusual host, a provider member of an unexpected
    /// shape — and answers null instead, because a context that cannot be described must not cost the entry.
    /// </summary>
    private static JsonNode? Read(Func<object?> read)
    {
        try
        {
            return ReadValue(read());
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Confirms the process started when it says it did, without faulting when it cannot be read.</summary>
    internal static DateTime ResolveProcessStartedUtc()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.StartTime.ToUniversalTime();
        }
        catch (Exception)
        {
            return DateTime.UtcNow;
        }
    }
}
