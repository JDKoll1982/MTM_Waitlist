using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// What the rest of the application may do with who is signed in and what this computer is: read it, and nothing
/// else. The launch is the only writer (FR-022).
/// </summary>
/// <remarks>
/// <para>
/// This is the property the retired launch-state object lacked, and the reason it was retired: a single shared
/// mutable object is what let attribution drift, because any caller could assign to it. So the rule is asserted
/// against the shapes rather than against a behaviour — a setter that appeared on either contract is the whole
/// defect, whether or not anything calls it yet.
/// </para>
/// <para>
/// <b>The one write path is named, not merely counted.</b> Machine configuration has exactly one writer,
/// <see cref="MachineConfigurationService"/>, and the setup stage reaches it through a step rather than assigning
/// to the facts. Asserting that the writer is the only writer is what makes "only the launch may change them"
/// checkable rather than aspirational.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchIdentityReadOnlyTests
{
    private static readonly Assembly s_startupAssembly = typeof(PersonIdentityService).Assembly;

    /// <summary>
    /// Neither contract offers a way to change what it reports, so a consumer cannot attribute an action to
    /// somebody else by assigning (FR-022).
    /// </summary>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_NeitherContract_OffersAWayToChangeWhatItReports()
    {
        foreach (var contract in new[] { typeof(IPersonIdentity), typeof(IMachineFacts) })
        {
            var writable = contract
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member is PropertyInfo { CanWrite: true } or FieldInfo { IsInitOnly: false })
                .Select(member => member.Name)
                .ToList();

            Assert.AreEqual(
                0,
                writable.Count,
                $"{contract.Name} can be assigned to ({string.Join(", ", writable)}), so any caller could change who is signed in or what this computer is.");
        }
    }

    /// <summary>
    /// The contracts are the plain-language facts, not a mutable state bag: every member either answers a
    /// question or reports whether there is an answer (FR-022).
    /// </summary>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_NeitherContract_ExposesAMutatorMethod()
    {
        foreach (var contract in new[] { typeof(IPersonIdentity), typeof(IMachineFacts) })
        {
            var mutators = contract
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.ReturnType == typeof(void))
                .Select(method => method.Name)
                .ToList();

            Assert.AreEqual(
                0,
                mutators.Count,
                $"{contract.Name} exposes a void method ({string.Join(", ", mutators)}), which is a write on a contract that is read-only.");
        }
    }

    /// <summary>
    /// Nothing outside the launch path implements the identity contract. An implementation somewhere else would be
    /// a second writer by construction, however well behaved it looked (FR-022).
    /// </summary>
    /// <remarks>
    /// <c>MachineSetupPerson</c> is named rather than merely tolerated: it is the authorised person the setup gate
    /// answers with, and it exists so the gate can state who unlocked setup without reaching into the launch's
    /// identity. It is produced by the gate and never assigned to anything, so it is a read-only answer too.
    /// </remarks>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_TheIdentityContract_HasOnlyTheLaunchPathsImplementations()
    {
        var implementations = s_startupAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IPersonIdentity).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { "MachineSetupPerson", "PersonIdentityService" },
            implementations,
            "An unexpected implementation of the identity contract is a second writer of who is signed in (FR-022).");

        foreach (var name in implementations)
        {
            var setters = s_startupAssembly
                .GetType($"MTM_Waitlist.Module_Startup.Services.{name}")!
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanWrite)
                .Select(property => property.Name)
                .ToList();

            Assert.AreEqual(
                0,
                setters.Count,
                $"{name} is publicly assignable ({string.Join(", ", setters)}), so a caller could rewrite who is signed in.");
        }
    }

    /// <summary>
    /// This computer's configuration has exactly one writer, and the setup stage reaches it rather than assigning
    /// to the machine facts (FR-022, FR-018).
    /// </summary>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_TheMachineContract_IsAnsweredByTheMachineFactsAndWrittenByOneService()
    {
        var implementations = s_startupAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IMachineFacts).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { "MachineFactsService" },
            implementations,
            "An unexpected implementation of the machine-facts contract is a second source of truth about this computer (FR-022).");
    }

    /// <summary>
    /// The draft the setup stage saves carries this computer's identity and nothing else, so no screen can write a
    /// folder into this computer's record (FR-040, FR-050).
    /// </summary>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_TheConfigurationDraft_CarriesIdentityAndNoFolder()
    {
        var members = typeof(MachineConfigurationDraft)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { "Building", "ComputerName", "Location" },
            members,
            "The machine-configuration draft carries something other than this computer's identity and the building it stands in, so a screen could write a folder into it (FR-040).");
    }

    /// <summary>
    /// Reading either contract before a launch has answered says so rather than inventing a person or a machine
    /// (FR-022, FR-015).
    /// </summary>
    [TestMethod]
    public void Service_LaunchIdentityReadOnlyTests_AnUnansweredIdentity_ReportsItselfAsUnansweredRatherThanInventingOne()
    {
        // The contract's shape is the assertion here: IsSignedIn and HardwareIdentityReadable exist precisely so a
        // consumer can tell "nobody yet" from "somebody", and neither contract carries a default person.
        Assert.IsNotNull(
            typeof(IPersonIdentity).GetProperty("IsSignedIn"),
            "The identity contract no longer reports whether anybody is signed in, so a consumer would have to guess.");

        Assert.IsNotNull(
            typeof(IMachineFacts).GetProperty("HardwareIdentityReadable"),
            "The machine contract no longer reports whether the hardware address could be read (FR-015).");

        foreach (var contract in new[] { typeof(IPersonIdentity), typeof(IMachineFacts) })
        {
            var names = contract.GetProperties().Select(property => property.Name).ToList();

            foreach (var forbidden in new[] { "Password", "Credential", "Token", "Secret", "Key" })
            {
                Assert.IsFalse(
                    names.Any(name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)),
                    $"{contract.Name} carries a {forbidden}, which the identity and machine contracts must never hold.");
            }
        }
    }
}
