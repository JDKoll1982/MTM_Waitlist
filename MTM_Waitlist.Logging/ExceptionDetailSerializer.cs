using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// Turns an exception into the entry's <c>exception_detail</c> and the fault into its
/// <c>error_fingerprint</c> (`contracts/logging-contract.md` §1.1, §1.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>One node per exception, in one column.</b> The chain is a JSON array, outermost first, and never child
/// rows: <c>ops_startup_logs</c> is a tamper-evident chain whose <c>entry_hash</c> covers one row, so spreading
/// an entry across tables would break the invariant the store exists to protect (plan D25). An
/// <see cref="AggregateException"/> contributes one node for each of its independent failures rather than being
/// reduced to its first.
/// </para>
/// <para>
/// <b>Serializing is defensive.</b> <see cref="Exception.Data"/> is open to any code, so a value is written
/// only through an allowlist of key names and value types, a key that names anything secret is dropped, and a
/// member that throws when it is read is replaced with a marker rather than being allowed to lose the entry.
/// </para>
/// <para>
/// <b>The fingerprint groups faults, it does not identify rows.</b> It is SHA-256, lower-case hexadecimal, over
/// the fault's type, the module, the action and a normalized shape of the message, so the same fault hashes the
/// same on any machine and in any run while a different fault does not (FR-033, SC-014). It is null for an entry
/// raised without an exception. The fuller normalization this file's task owns — replacing identifiers as well
/// as digits and quoted values — is deferred; what is here is deliberately conservative, because
/// over-normalizing joins two faults the panel should keep apart.
/// </para>
/// </remarks>
public static class ExceptionDetailSerializer
{
    /// <summary>The most exception nodes one entry's chain may carry before it is cut off.</summary>
    public const int MaxNodes = 20;

    /// <summary>The most custom data pairs written for one node.</summary>
    public const int MaxDataEntriesPerNode = 32;

    /// <summary>The most characters kept from one string value or member.</summary>
    public const int MaxValueLength = 2048;

    /// <summary>The marker written in place of a member that could not be read.</summary>
    public const string UnreadableMarker = "<unreadable>";

    /// <summary>The marker written in place of a member that was not set.</summary>
    public const string AbsentMarker = "<none>";

    private const string TruncatedKey = "truncated";
    private const string FullKey = "full";

    /// <summary>
    /// Key-name fragments that mean the value beside them is not written, whatever it holds.
    /// </summary>
    /// <remarks>
    /// The authoritative list of what may never reach the store is `contracts/logging-contract.md` §6. A
    /// deny-list by name is deliberately blunt: a custom data key called <c>connectionName</c> is dropped along
    /// with a credential, because the cost of dropping a diagnostic note is an incomplete entry while the cost
    /// of keeping one is a credential in a store built to be copied out of a panel.
    /// </remarks>
    private static readonly string[] s_deniedKeyFragments =
    [
        "password", "passwd", "pwd", "secret", "token", "credential", "salt", "key", "pin", "connection",
    ];

    /// <summary>
    /// Serializes an exception and its complete chain into the entry's <c>exception_detail</c>.
    /// </summary>
    /// <param name="exception">The fault, or null for an entry raised without one.</param>
    /// <returns>
    /// A JSON array whose first node is the outermost exception, or null when there is no fault to serialize.
    /// The first node also carries the fault's <c>ToString()</c> as <c>full</c>, so a reader gets the familiar
    /// representation, and <c>truncated: true</c> when the chain was cut at a node boundary.
    /// </returns>
    public static string? Serialize(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        try
        {
            var nodes = new JsonArray();
            var remaining = MaxNodes;
            var truncated = false;

            AppendNode(exception, level: 0, index: 0, nodes, ref remaining, ref truncated);

            if (nodes.Count > 0 && nodes[0] is JsonObject first)
            {
                first[FullKey] = ReadMember(exception.ToString, AbsentMarker);

                if (truncated)
                {
                    first[TruncatedKey] = true;
                }
            }

            return nodes.ToJsonString();
        }
        catch (Exception)
        {
            // The serializer must never be the reason an entry is lost. A chain that cannot be described at all
            // still leaves a marker, so a reader can see that a fault was recorded and that its description was
            // unavailable.
            return new JsonObject
            {
                [AbsentMarker] = "the exception chain could not be serialized",
                [TruncatedKey] = true,
            }.ToJsonString();
        }
    }

    /// <summary>
    /// Derives the grouping hash for a fault (`contracts/logging-contract.md` §1.2).
    /// </summary>
    /// <param name="errorType">The fault's type.</param>
    /// <param name="module">The module the entry came from.</param>
    /// <param name="action">The operation under way.</param>
    /// <param name="message">The fault's message, whose variable parts are normalized out first.</param>
    /// <returns>Lower-case hexadecimal SHA-256, or null when there is no fault to group.</returns>
    public static string? ComputeFingerprint(string? errorType, string? module, string? action, string? message)
    {
        if (string.IsNullOrWhiteSpace(errorType))
        {
            return null;
        }

        var shape = string.Join(
            '\u001f',
            (errorType ?? string.Empty).Trim().ToLowerInvariant(),
            (module ?? string.Empty).Trim().ToLowerInvariant(),
            (action ?? string.Empty).Trim().ToLowerInvariant(),
            NormalizeMessage(message));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(shape));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Reduces a message to its shape: quoted values become a placeholder and digit runs become a placeholder, so
    /// two occurrences of one fault hash alike while two faults do not.
    /// </summary>
    /// <param name="message">The message to normalize.</param>
    /// <returns>The normalized shape, lower-cased with runs of whitespace collapsed.</returns>
    public static string NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(message.Length);
        var index = 0;

        while (index < message.Length)
        {
            var current = message[index];

            if (current is '"' or '\'')
            {
                var closing = message.IndexOf(current, index + 1);
                if (closing < 0)
                {
                    index++;
                    continue;
                }

                normalized.Append("<value>");
                index = closing + 1;
                continue;
            }

            if (char.IsDigit(current))
            {
                while (index < message.Length && char.IsDigit(message[index]))
                {
                    index++;
                }

                normalized.Append("<n>");
                continue;
            }

            normalized.Append(char.IsWhiteSpace(current) ? ' ' : char.ToLowerInvariant(current));
            index++;
        }

        return CollapseWhitespace(normalized.ToString()).Trim();
    }

    private static void AppendNode(
        Exception exception,
        int level,
        int index,
        JsonArray nodes,
        ref int remaining,
        ref bool truncated)
    {
        if (remaining <= 0)
        {
            truncated = true;
            return;
        }

        remaining--;

        var node = new JsonObject
        {
            ["level"] = level,
            ["index"] = index,
            ["type"] = ReadMember(() => exception.GetType().FullName, AbsentMarker),
            ["message"] = ReadMember(() => exception.Message, AbsentMarker),
            ["stackTrace"] = ReadMember(() => exception.StackTrace, AbsentMarker),
            ["source"] = ReadMember(() => exception.Source, AbsentMarker),
            ["hresult"] = ReadHResult(exception),
            ["helpLink"] = ReadMember(() => exception.HelpLink, AbsentMarker),
            ["targetSite"] = ReadTargetSite(exception),
            ["isRootCause"] = IsLeaf(exception),
            ["data"] = ReadData(exception),
        };

        nodes.Add(node);

        var childIndex = 0;
        foreach (var child in Children(exception))
        {
            AppendNode(child, level + 1, childIndex, nodes, ref remaining, ref truncated);
            childIndex++;
        }
    }

    private static IEnumerable<Exception> Children(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            // An aggregate carries several independent failures; all of them are kept, not just the first.
            IReadOnlyList<Exception> inner;
            try
            {
                inner = aggregate.InnerExceptions;
            }
            catch (Exception)
            {
                yield break;
            }

            for (var childIndex = 0; childIndex < inner.Count; childIndex++)
            {
                var child = inner[childIndex];
                if (child is not null)
                {
                    yield return child;
                }
            }

            yield break;
        }

        Exception? single = null;
        try
        {
            single = exception.InnerException;
        }
        catch (Exception)
        {
            // A hostile override of InnerException loses its chain link, not the entry.
        }

        if (single is not null)
        {
            yield return single;
        }
    }

    /// <summary>
    /// Whether the node has no child in the chain. The deepest node is useful to know but is not proof of the
    /// root cause, which is why the flag is named for the chain and not for the diagnosis.
    /// </summary>
    private static bool IsLeaf(Exception exception)
    {
        try
        {
            return exception is AggregateException aggregate
                ? aggregate.InnerExceptions.Count == 0
                : exception.InnerException is null;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static JsonObject ReadData(Exception exception)
    {
        var data = new JsonObject();
        var written = 0;

        IDictionary? entries;
        try
        {
            entries = exception.Data;
        }
        catch (Exception)
        {
            return data;
        }

        if (entries is null)
        {
            return data;
        }

        try
        {
            foreach (DictionaryEntry entry in entries)
            {
                if (written >= MaxDataEntriesPerNode)
                {
                    data[TruncatedKey] = true;
                    break;
                }

                var name = entry.Key?.ToString();
                if (string.IsNullOrWhiteSpace(name) || IsDenied(name))
                {
                    continue;
                }

                if (!TryDescribeValue(entry.Value, out var described))
                {
                    continue;
                }

                data[name] = described;
                written++;
            }
        }
        catch (Exception)
        {
            // A data dictionary that throws while it is enumerated leaves what was already read, which is
            // strictly better than losing the entry.
        }

        return data;
    }

    private static bool IsDenied(string name) =>
        s_deniedKeyFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static bool TryDescribeValue(object? value, out JsonNode? described)
    {
        described = null;

        switch (value)
        {
            case null:
                described = null;
                return true;
            case string text:
                described = Truncate(text);
                return true;
            case bool flag:
                described = JsonValue.Create(flag);
                return true;
            case Guid guid:
                described = guid.ToString();
                return true;
            case DateTime timestamp:
                described = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                return true;
            case DateTimeOffset offset:
                described = offset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                return true;
            case TimeSpan duration:
                described = duration.ToString("c", CultureInfo.InvariantCulture);
                return true;
            case Enum enumValue:
                described = enumValue.ToString();
                return true;
            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                described = JsonValue.Create(value.ToString());
                return true;
            default:
                // Any other type is described by its type name only: a value of unknown shape is exactly the
                // kind of thing that turns out to be a credential or a connection string.
                described = $"<{value.GetType().FullName}>";
                return true;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxValueLength ? value : string.Concat(value.AsSpan(0, MaxValueLength), "…");

    private static string ReadHResult(Exception exception)
    {
        try
        {
            return exception.HResult.ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return UnreadableMarker;
        }
    }

    private static string ReadTargetSite(Exception exception)
    {
        try
        {
            MethodBase? target = exception.TargetSite;
            return target is null
                ? AbsentMarker
                : $"{target.DeclaringType?.FullName}.{target.Name}";
        }
        catch (Exception)
        {
            return UnreadableMarker;
        }
    }

    private static string ReadMember(Func<string?> read, string fallback)
    {
        try
        {
            var value = read();
            return string.IsNullOrWhiteSpace(value) ? fallback : Truncate(value);
        }
        catch (Exception)
        {
            return UnreadableMarker;
        }
    }

    private static string CollapseWhitespace(string value)
    {
        var collapsed = new StringBuilder(value.Length);
        var previousWasSpace = false;

        foreach (var character in value)
        {
            if (character == ' ')
            {
                if (!previousWasSpace)
                {
                    collapsed.Append(character);
                }

                previousWasSpace = true;
                continue;
            }

            collapsed.Append(character);
            previousWasSpace = false;
        }

        return collapsed.ToString();
    }
}
