using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Fixtures;

/// <summary>
/// A settable machine-facts double for the tests that are about a consumer rather than about the machine.
/// </summary>
/// <remarks>
/// The production contract is read-only (FR-022), and the production implementation reads the running machine
/// and the store, neither of which a unit test should touch. This fake carries the five facts with setters, so a
/// test can state the machine it wants to describe — registered, unregistered, or with an identity that could not
/// be read — without a store and without matching the developer's own hardware.
/// </remarks>
public sealed class FakeMachineFacts : IMachineFacts
{
    public string Hostname { get; set; } = "test-workstation";

    public string? MacAddress { get; set; }

    public ComputerRecord? RegisteredComputer { get; set; }

    public bool IsRegistered { get; set; }

    public bool HardwareIdentityReadable { get; set; }
}
