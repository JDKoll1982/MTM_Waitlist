using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The request reads name the material handler, so the routines that feed the card must return a name beside the
/// employee number the queue stores (FR-022).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is asserted against the artifacts.</b> The card used to read the employee number where a person's
/// name belongs, because the queue's assignee column holds the number and the read returned nothing else. The fix
/// is in SQL, which no unit test can exercise without a store — and this repository has already shipped a routine
/// that was correct on disk, stale in the store, and invisible at run time (see
/// <c>Database/CopilotScripts/refresh_schema_drift.ps1</c>). So the shape that makes the fix real is pinned here:
/// both routines must return the resolved name, and the aggregate an installer applies must carry the same text,
/// because the aggregate is what a machine actually applies.
/// </para>
/// <para>
/// The name is resolved from the entry the handler wrote, matched to the current handler's own number, which is
/// what stops a later entry by somebody else from naming the wrong person. Both of those facts are asserted —
/// a routine that returned any audit name regardless of author would reintroduce the defect in a subtler form.
/// </para>
/// </remarks>
[TestClass]
public sealed class HandlerNameStoreReadTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    private static string Procedures(string name) => Path.Combine(Root, "Database", "StoredProcedures", name, "create.sql");

    [TestMethod]
    public void Database_HandlerNameStoreReadTests_TheListRead_ResolvesTheHandlersNameFromTheirOwnEntry()
    {
        var routine = File.ReadAllText(Procedures("sp_waitlist_request_list"));

        StringAssert.Contains(
            routine,
            "AS assigned_material_handler_name",
            "The list read the card is built from no longer returns the handler's name (FR-022).");
        StringAssert.Contains(
            routine,
            "a.actor_employee_number = q.assigned_material_handler",
            "The handler's name must be matched to the current handler's own number, or a later entry by somebody else could name the wrong person.");
        StringAssert.Contains(
            routine,
            "a.actor_employee_name IS NOT NULL",
            "An entry with no author is a system write and names nobody.");
    }

    [TestMethod]
    public void Database_HandlerNameStoreReadTests_TheSingleRequestRead_ResolvesTheHandlersNameToo()
    {
        var routine = File.ReadAllText(Procedures("sp_waitlist_request_get"));

        StringAssert.Contains(
            routine,
            "AS assigned_material_handler_name",
            "The single-request read must carry the same shape as the list read.");
        StringAssert.Contains(
            routine,
            "a.actor_employee_number = waitlist_requests_queue.assigned_material_handler",
            "And resolve the name the same way, from the handler's own entry.");
    }

    [TestMethod]
    public void Database_HandlerNameStoreReadTests_TheAggregateAnInstallerApplies_CarriesTheSameResolution()
    {
        var aggregate = File.ReadAllText(Path.Combine(Root, "Database", "StoredProcedures", "AllSPs.sql"));

        var resolved = aggregate.Split("AS assigned_material_handler_name").Length - 1;

        Assert.AreEqual(
            2,
            resolved,
            "AllSPs.sql is what a machine applies; both request reads must carry the handler-name resolution in it, or an installed store keeps printing the employee number.");
    }
}
