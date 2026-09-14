using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Reads one person from <c>core_users_profiles</c> through <c>sp_core_employee_by_identifier_get</c> — the
/// lookup the request flow attributes a request by (FR-046, constitution III: every data operation goes through a
/// stored procedure, and there is no inline statement text here).
/// </summary>
public sealed class EmployeeDirectoryService : IEmployeeDirectoryService
{
    private const string EmployeeByIdentifierProcedure = "sp_core_employee_by_identifier_get";

    private readonly IMySqlHelperServer? _mySqlHelperServer;

    public EmployeeDirectoryService(IMySqlHelperServer? mySqlHelperServer = null)
    {
        _mySqlHelperServer = mySqlHelperServer;
    }

    public async Task<EmployeeIdentity?> FindByEmployeeIdentifierAsync(string employeeIdentifier, CancellationToken cancellationToken = default)
    {
        var normalized = (employeeIdentifier ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            // An empty identifier is not an outage — there is nothing to look up, and the caller's own rule
            // refuses it with the message the person needs.
            return null;
        }

        if (_mySqlHelperServer is null)
        {
            // Answering null here would report a missing database helper as "no such employee", which is the
            // wrong refusal: nobody asked for it and the person cannot act on it (FR-026).
            throw new InvalidOperationException("The employee directory cannot be read because no database helper is configured.");
        }

        var rows = await _mySqlHelperServer.ExecuteStoredProcedureQueryAsync(
            EmployeeByIdentifierProcedure,
            new Dictionary<string, object?> { ["p_employee_identifier"] = normalized },
            MySqlDatabaseTarget.MtmWaitlist,
            cancellationToken).ConfigureAwait(false);

        if (rows.Count == 0)
        {
            StartupDebugLog.Info("EmployeeDirectory", $"No account carries the employee identifier '{normalized}'.");
            return null;
        }

        var row = rows[0];
        var storedIdentifier = ReadString(row, "employee_identifier");

        // The store answered with a row, but the identifier it carries is the evidence — not the name, and not
        // the fact that a row came back (FR-046).
        if (!string.Equals(storedIdentifier, normalized, StringComparison.OrdinalIgnoreCase))
        {
            StartupDebugLog.Info(
                "EmployeeDirectory",
                $"The lookup for '{normalized}' answered with an account carrying '{storedIdentifier}'; the identifiers disagree, so no identity is returned.");
            return null;
        }

        var identity = new EmployeeIdentity
        {
            EmployeeNumber = storedIdentifier,
            DisplayName = ReadString(row, "display_name"),
            IsActive = ReadBool(row, "is_active"),
        };

        StartupDebugLog.Info(
            "EmployeeDirectory",
            $"Resolved employee '{identity.EmployeeNumber}' as '{identity.DisplayName}' (active={identity.IsActive}).");

        return identity;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? Convert.ToString(value)?.Trim() ?? string.Empty : string.Empty;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var value) && value is not null)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                // A store that answered with something that is not a flag is treated as "not active", so the
                // caller refuses the request rather than attributing it to an account it could not read.
            }
        }

        return false;
    }
}
