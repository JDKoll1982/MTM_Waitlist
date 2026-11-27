using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The start-up connection requirement (SEC-001; FR-019, SC-001).
/// </summary>
/// <remarks>
/// The requirement is stated here against a host the test describes, through an injected reader, so the assertions
/// are about the rule rather than about whichever variables this developer's machine happens to carry.
/// </remarks>
[TestClass]
public sealed class ConfigurationRequirementTests
{
    private const string NamedVariable = "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING";

    [TestMethod]
    public void Service_ConfigurationRequirementTests_WaitlistStoreWhenNoValueIsSuppliedRefusesAndNamesTheVariable()
    {
        // Arrange: a host with nothing in its environment and nothing in its saved settings.
        var settings = new WaitlistDatabaseOptions { ConnectionStringEnvironmentVariable = NamedVariable };

        // Act
        var requirement = ConfigurationRequirement.WaitlistStore(settings, _ => null);

        // Assert: the launch must not go on, the value is absent rather than blank, and the person is told which
        // variable supplies it (FR-019).
        Assert.IsFalse(requirement.IsSatisfied, "a host with no value must be refused");
        Assert.IsNull(requirement.Value, "a refused host must not fall back to an empty string");
        StringAssert.Contains(requirement.RefusalMessage, NamedVariable, "the refusal must name the missing variable");
    }

    [TestMethod]
    public void Service_ConfigurationRequirementTests_WaitlistStoreWhenOnlyWhitespaceIsSuppliedRefusesRatherThanUsingIt()
    {
        // Arrange: a variable that exists but carries nothing usable.
        var settings = new WaitlistDatabaseOptions { ConnectionStringEnvironmentVariable = NamedVariable };

        // Act
        var requirement = ConfigurationRequirement.WaitlistStore(settings, _ => "   ");

        // Assert
        Assert.IsFalse(requirement.IsSatisfied, "whitespace is not a connection");
        Assert.IsNull(requirement.Value, "a blank value must not become the connection");
    }

    [TestMethod]
    public void Service_ConfigurationRequirementTests_WaitlistStoreWhenTheVariableIsSetIsSatisfiedWithThatValue()
    {
        // Arrange
        var settings = new WaitlistDatabaseOptions { ConnectionStringEnvironmentVariable = NamedVariable };
        const string supplied = "Server=localhost;Database=mtm_waitlist;User ID=nobody;Password=nothing;";

        // Act
        var requirement = ConfigurationRequirement.WaitlistStore(
            settings,
            name => string.Equals(name, NamedVariable, StringComparison.Ordinal) ? supplied : null);

        // Assert
        Assert.IsTrue(requirement.IsSatisfied, "a host with the variable set starts normally");
        Assert.AreEqual(supplied, requirement.Value);
    }

    [TestMethod]
    public void Service_ConfigurationRequirementTests_WaitlistStoreWhenOnlyThisComputersSettingsSupplyAConnectionIsSatisfied()
    {
        // Arrange: the shape every machine that saves its own connection keeps.
        var settings = new WaitlistDatabaseOptions
        {
            ConnectionStringEnvironmentVariable = NamedVariable,
            ConnectionString = "Server=localhost;Database=mtm_waitlist;",
        };

        // Act
        var requirement = ConfigurationRequirement.WaitlistStore(settings, _ => null);

        // Assert
        Assert.IsTrue(requirement.IsSatisfied);
        Assert.AreEqual(settings.ConnectionString, requirement.Value);
    }
}
