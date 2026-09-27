using System.Net.NetworkInformation;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The machine facts contract's implementation: this machine's own identity, and its registry row.
/// </summary>
/// <remarks>
/// <para>
/// <b>The hostname and the hardware address are read from the machine itself</b>, not from the store, because
/// they are the facts the machine presents for matching. The registry row is then read from the store by name
/// through <c>sp_core_computers_registry_lookup_by_name_get</c>, and that row is accepted only when the address
/// it holds equals the one read from this machine, so the name-and-address pair is still what identifies it.
/// </para>
/// <para>
/// <b>An unreadable hardware identity is admitted (FR-015).</b> When no usable address can be read,
/// <see cref="MacAddress"/> is <c>null</c>, <see cref="HardwareIdentityReadable"/> is false, and no registry
/// read is attempted — a fact that could not be read is not a failed check, so the gate reports it could not be
/// performed rather than refusing the machine.
/// </para>
/// <para>
/// <b>Read-only, with one writer.</b> <see cref="RefreshAsync"/> and <see cref="Apply"/> are for the launch
/// pipeline. Consumers depend on <see cref="IMachineFacts"/>, which has no setter, and a component that must
/// change the machine's configuration calls <c>MachineConfigurationService</c>.
/// </para>
/// </remarks>
public sealed class MachineFactsService : IMachineFacts
{
    /// <summary>The store read that resolves one machine by name, before its hardware address confirms it.</summary>
    private const string LookupByNameProcedure = "sp_core_computers_registry_lookup_by_name_get";

    private readonly IMySqlHelperServer _mySqlHelperServer;
    private readonly string _hostname;
    private readonly string? _macAddress;

    private ComputerRecord? _registeredComputer;

    public MachineFactsService(IMySqlHelperServer mySqlHelperServer)
    {
        _mySqlHelperServer = mySqlHelperServer ?? throw new ArgumentNullException(nameof(mySqlHelperServer));
        _hostname = Environment.MachineName.Trim();
        _macAddress = ReadHardwareAddress();
    }

    /// <inheritdoc />
    public string Hostname => _hostname;

    /// <inheritdoc />
    public string? MacAddress => _macAddress;

    /// <inheritdoc />
    public ComputerRecord? RegisteredComputer => _registeredComputer;

    /// <inheritdoc />
    public bool IsRegistered => _registeredComputer?.IsRegistered == true;

    /// <inheritdoc />
    public bool HardwareIdentityReadable => !string.IsNullOrWhiteSpace(_macAddress);

    /// <summary>
    /// Reads this machine's registry row and keeps it. Called by the launch pipeline's machine check; nothing
    /// else writes here.
    /// </summary>
    /// <param name="cancellationToken">Cancels the store read.</param>
    /// <returns><see langword="true"/> when a registered row was found.</returns>
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!HardwareIdentityReadable)
        {
            // No usable hardware address means the identity read cannot be performed at all. Nothing is
            // attempted, because a read would only be a guess, and the machine is admitted on the unreadable
            // fact rather than refused for it (FR-015).
            _registeredComputer = null;
            return false;
        }

        var rows = await _mySqlHelperServer
            .ExecuteStoredProcedureQueryAsync(
                LookupByNameProcedure,
                new Dictionary<string, object?>
                {
                    ["p_name"] = _hostname,
                },
                MySqlDatabaseTarget.MtmWaitlist,
                cancellationToken)
            .ConfigureAwait(false);

        var row = Map(rows.FirstOrDefault());

        // The name resolves the candidate and the hardware address confirms it. A row holding a different
        // address is another machine that happens to share a name, so it is not this machine's record.
        _registeredComputer = string.Equals(row?.MacAddressNormalized, _macAddress, StringComparison.OrdinalIgnoreCase)
            ? row
            : null;

        return IsRegistered;
    }

    /// <summary>
    /// Applies the registry row the pipeline resolved. The launch pipeline is the only caller.
    /// </summary>
    /// <param name="registeredComputer">The row, or <c>null</c> when this machine is not registered.</param>
    public void Apply(ComputerRecord? registeredComputer) => _registeredComputer = registeredComputer;

    /// <summary>
    /// This machine's hardware address, normalised to the form the store holds it in: lower case, hyphen
    /// separated, one two-character group per byte.
    /// </summary>
    /// <remarks>
    /// The first operational, physical adapter that reports a six-byte address wins. A virtual, tunnel or
    /// loopback adapter is skipped because its address is not this machine's hardware identity; an adapter with
    /// no address at all is skipped because it presents nothing to match on.
    /// </remarks>
    private static string? ReadHardwareAddress()
    {
        NetworkInterface? candidate = null;

        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up
                    || networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback
                        or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var address = networkInterface.GetPhysicalAddress();

                if (address is null || address.GetAddressBytes().Length != 6)
                {
                    continue;
                }

                candidate = networkInterface;
                break;
            }

            if (candidate is null)
            {
                return null;
            }

            return Normalize(candidate.GetPhysicalAddress().GetAddressBytes());
        }
        catch (NetworkInformationException)
        {
            // No adapter could be enumerated at all, which is exactly the "could not be read" case rather than
            // an error worth stopping a launch for.
            return null;
        }
    }

    private static string Normalize(byte[] bytes) =>
        string.Join('-', bytes.Select(value => value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));

    private static ComputerRecord? Map(IReadOnlyDictionary<string, object?>? row)
    {
        if (row is null)
        {
            return null;
        }

        return new ComputerRecord
        {
            Id = ReadInt64(row, "id"),
            ComputerName = ReadString(row, "computer_name"),
            DisplayName = ReadString(row, "display_name"),
            Description = ReadString(row, "description"),
            MacAddressNormalized = ReadString(row, "mac_address_normalized"),
            IsRegistered = ReadInt64(row, "is_registered") == 1,
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static long ReadInt64(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        // TINYINT(1)/BIT columns are surfaced by the MySQL driver as a bool, and Convert.ToInt64 on a bool
        // throws InvalidCastException, so the type is guarded BEFORE converting rather than caught afterwards.
        if (value is bool boolValue)
        {
            return boolValue ? 1 : 0;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        try
        {
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return 0;
        }
    }
}
