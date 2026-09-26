using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The computer this application is running on, for reading only (FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>Read-only, with one writer.</b> There is no setter on this contract. The launch pipeline is the only
/// writer, and it writes through <c>MachineConfigurationService</c> and <c>MachineFactsService</c> rather than
/// through this interface. A screen that must change the machine's configuration calls the owning service; it
/// never assigns here.
/// </para>
/// <para>
/// <b>An unreadable fact is not a failed check (FR-015).</b> A machine whose hardware identity cannot be read is
/// admitted, so <see cref="HardwareIdentityReadable"/> being false does not make <see cref="IsRegistered"/> false
/// and does not by itself refuse anybody. Losing configuration resolves to unconfigured at the next check rather
/// than degrading into running without it (FR-006, FR-009).
/// </para>
/// </remarks>
public interface IMachineFacts
{
    /// <summary>
    /// This machine's own hostname, normalised. It is what the registry row's <c>computer_name</c> is matched
    /// against, and it is the fallback the work-centre picker names when no resolved hostname is held.
    /// </summary>
    string Hostname { get; }

    /// <summary>
    /// This machine's hardware address, normalised to the form the store holds (lower-case, hyphen separated),
    /// or <c>null</c> when no usable address could be read. Matched on
    /// <c>core_computers_registry.mac_address_normalized</c>.
    /// </summary>
    string? MacAddress { get; }

    /// <summary>
    /// The machine's row from <c>core_computers_registry</c>, or <c>null</c> when this machine is not registered.
    /// The existing <see cref="ComputerRecord"/> is reused rather than replaced: it already carries the id, the
    /// computer name, the display name, the description, the normalised address and the registered flag, which is
    /// every fact the machine-configuration screen and the shell need.
    /// </summary>
    ComputerRecord? RegisteredComputer { get; }

    /// <summary>
    /// Whether this machine's configuration is registered, confirmed by the match against the registry row.
    /// </summary>
    bool IsRegistered { get; }

    /// <summary>
    /// Whether a usable hardware address could be read from this machine. False means the hardware identity
    /// check could not be performed, which is reported as such rather than as a refusal (FR-015).
    /// </summary>
    bool HardwareIdentityReadable { get; }
}
