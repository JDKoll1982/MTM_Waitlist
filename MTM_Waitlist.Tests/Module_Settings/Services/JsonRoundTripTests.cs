using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The stored filter was written by the reflection-based serializer before this change and is read by the
/// generated metadata after it. A stored value that stops round-tripping is a user's own list preference
/// silently reverting, which is why it is asserted against the old wire shape rather than against a freshly
/// written value.
/// </summary>
[TestClass]
public sealed class JsonRoundTripTests
{
    /// <remarks>
    /// The literal below is the shape the previous release wrote: PascalCase member names, with an unsaved half
    /// present as null rather than omitted. Both halves must survive a read, and the derived
    /// <see cref="UserListFilter.IsApplied"/> value must follow them.
    /// </remarks>
    [TestMethod]
    public void Service_JsonRoundTripTests_TheOldStoredShapeReadsWithBothHalvesIntact()
    {
        const string WrittenByThePreviousRelease = "{\"Search\":\" 12g \",\"RoleCode\":null}";

        var outcome = UserListFilter.TryRead(WrittenByThePreviousRelease, out var filter);

        Assert.AreEqual(UserListFilter.FilterReadOutcome.Read, outcome);
        Assert.AreEqual("12g", filter.SearchText, "The search half is read and trimmed.");
        Assert.AreEqual(string.Empty, filter.RoleCode, "An unsaved half is an absent half, not a lost one.");
        Assert.IsTrue(filter.IsApplied, "A filter with a search text is applied.");
    }

    /// <remarks>A value saved by this release must be readable by the same route, so a downgrade is not a loss.</remarks>
    [TestMethod]
    public void Service_JsonRoundTripTests_AValueWrittenNowReadsBackUnchanged()
    {
        var original = new UserListFilter("380397", "Viewer");

        var outcome = UserListFilter.TryRead(original.ToStoredValue(), out var read);

        Assert.AreEqual(UserListFilter.FilterReadOutcome.Read, outcome);
        Assert.AreEqual(original.SearchText, read.SearchText);
        Assert.AreEqual(original.RoleCode, read.RoleCode);
    }
}
