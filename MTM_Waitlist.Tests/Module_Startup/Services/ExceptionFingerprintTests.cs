using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Logging;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The fingerprint: two occurrences of one fault group together and two different faults do not
/// (`contracts/logging-contract.md` §1.2; FR-033, SC-014).
/// </summary>
/// <remarks>
/// The fingerprint is what the panel groups by, so the promise has two halves that have to hold at once: the
/// same fault hashes the same on any machine and in any run, and a different fault does not. A test that only
/// proved the first half would pass for a constant.
/// </remarks>
[TestClass]
public sealed class ExceptionFingerprintTests
{
    [TestMethod]
    public void ComputeFingerprint_WhenTheSameFaultIsRaisedTwice_ProducesTheSameValue()
    {
        // Arrange: the same fault raised on two machines, at two times, with two values.
        var first = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save-machine-configuration",
            "Duplicate entry 'Shop floor station' for key 'display_name'");
        var second = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save-machine-configuration",
            "Duplicate entry 'Line 4 press' for key 'display_name'");

        // Assert: SC-014 — the two occurrences are found as one group.
        Assert.IsNotNull(first);
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void ComputeFingerprint_WhenTheMessageDiffersInMoreThanItsValues_ProducesADifferentValue()
    {
        // Arrange
        var refusedName = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save-machine-configuration",
            "Duplicate entry 'a machine' for key 'display_name'");
        var refusedShare = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save-machine-configuration",
            "The picture share could not be reached");

        // Assert: over-normalizing would join two faults the panel should keep apart.
        Assert.AreNotEqual(refusedName, refusedShare);
    }

    [TestMethod]
    public void ComputeFingerprint_WhenOnlyTheModuleOrTheTypeDiffers_ProducesADifferentValue()
    {
        // Arrange
        const string message = "The store did not answer";
        var inStartup = ExceptionDetailSerializer.ComputeFingerprint(nameof(InvalidOperationException), "Startup", "launch", message);
        var inSettings = ExceptionDetailSerializer.ComputeFingerprint(nameof(InvalidOperationException), "Settings", "launch", message);
        var anotherType = ExceptionDetailSerializer.ComputeFingerprint(nameof(TimeoutException), "Startup", "launch", message);

        // Assert
        Assert.AreNotEqual(inStartup, inSettings);
        Assert.AreNotEqual(inStartup, anotherType);
    }

    [TestMethod]
    public void ComputeFingerprint_WhenAParameterValueIsInTheMessage_DoesNotCarryTheValue()
    {
        // Arrange: the same fault twice, differing only in the value a provider put in the message.
        var first = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save",
            "Parameter @p_secret=one-value rejected");
        var sameShape = ExceptionDetailSerializer.ComputeFingerprint(
            nameof(InvalidOperationException),
            "Startup",
            "save",
            "Parameter @p_secret=another-value rejected");

        // Assert: the value is not part of what is stored, so it cannot split a group (§6, FR-036).
        Assert.AreEqual(first, sameShape);
    }

    [TestMethod]
    public void ComputeFingerprint_WhenThereIsNoFault_CarriesNone()
    {
        // Assert: an entry raised without an exception is grouped with nothing (`contracts/logging-contract.md` §1.2).
        Assert.IsNull(ExceptionDetailSerializer.ComputeFingerprint(null, "Startup", "launch", "the launch carried on"));
    }

    [TestMethod]
    public void RedactMessage_WhenNoParameterIsPresent_LeavesTheMessageWhole()
    {
        // Arrange
        const string message = "The store did not answer";

        // Assert: a fault with no value in it loses nothing.
        Assert.AreEqual(message, ExceptionDetailSerializer.RedactMessage(message));
    }

    [TestMethod]
    public void RedactMessage_WhenAParameterCarriesAValue_CutsAtTheEqualsAndNamesTheParameter()
    {
        // Arrange
        const string message = "Duplicate entry for @p_display_name=Shop floor station rejected";

        // Act
        var redacted = ExceptionDetailSerializer.RedactMessage(message);

        // Assert: cut at the '=', the name recorded, the value gone (§6, FR-036).
        Assert.IsFalse(redacted.Contains("Shop floor station", StringComparison.Ordinal));
        StringAssert.Contains(redacted, "@p_display_name");
        StringAssert.Contains(redacted, "1 recorded");
    }
}
