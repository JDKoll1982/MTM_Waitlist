using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Services.DependencyInjection;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The startup module's registrations, checked for the one property no other test can see: that the container
/// can actually build every type the module registers.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because a launch died on it.</b> On 2026-09-27 the first real launch of the rebuilt surface
/// ended before its first step with "A suitable constructor for type
/// 'MTM_Waitlist.Module_Startup.Services.ReadHardwareIdentityStep' could not be located." Nineteen registered
/// types carried <c>internal</c> constructors: the types are <c>internal</c> by design, and the constructors
/// followed the type, but a container resolves a type through its <b>public</b> constructors only. Nothing in the
/// suite noticed, because every test built the type it was about by hand and none went through the container.
/// </para>
/// <para>
/// <b>What it asserts, and what it deliberately does not.</b> It asks the container's own descriptor list which
/// implementation types the module registers, and refuses any type with no public constructor. It does not build
/// the graph: a type whose dependencies are missing is a different defect, found by running the application, and
/// asserting it here would mean standing up a store. This catches exactly the mistake that was made.
/// </para>
/// </remarks>
[TestClass]
public sealed class ModuleRegistrationTests
{
    [TestMethod]
    public void EveryStartupRegistration_HasAConstructorTheContainerCanUse()
    {
        // Arrange: the module's own registrations, into a bare collection.
        var services = new ServiceCollection();
        services.AddStartupModuleServices(new ConfigurationBuilder().Build());

        var registered = services
            .Where(descriptor => descriptor.ImplementationType is not null)
            .Select(descriptor => descriptor.ImplementationType!)
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        Assert.IsTrue(registered.Count >= 20, $"the module registered only {registered.Count} types, so this check is not looking at the module");

        // Act: which of them the container cannot construct.
        var unconstructable = registered
            .Where(type => type.GetConstructors().Length == 0)
            .Select(type => $"{type.FullName} (registered as {type.Name})")
            .ToList();

        // Assert: none. A public constructor on an internal type is visible to the container, which is why the
        // types stay internal and only their constructors changed.
        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            unconstructable,
            "these registered types have no public constructor, so the container cannot build them: "
                + string.Join("; ", unconstructable));
    }
}
