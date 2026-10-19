using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The transition engine, proven against the artifact that ships: every assertion below names a statement in
/// <c>Database/StoredProcedures/sp_waitlist_lugger_locations_status_set/create.sql</c> and fails if that statement is
/// removed (FR-012, FR-015, FR-016, FR-017, FR-019, FR-020).
/// </summary>
/// <remarks>
/// The engine is SQL, so what a unit test can hold it to is the set of statements it is made of. Its behaviour is
/// proven end to end against a store by the wave that builds it; this file is the gate that stops a later edit
/// quietly deleting the reason rule, the row locks or the sort order.
/// </remarks>
[TestClass]
public sealed class LuggerTransitionStoreRuleTests
{
    private static string StatusSetScript => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Database", "StoredProcedures", "sp_waitlist_lugger_locations_status_set", "create.sql"));

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_AcceptsTheFourPinnedStatusesAndRefusesAnythingElse()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains(
                "BINARY p_status NOT IN (BINARY 'Available', BINARY 'Backup', BINARY 'Full', BINARY 'Do Not Use')",
                StringComparison.Ordinal),
            "The status guard is what makes 'full' or 'Broken' a refusal rather than a fifth status.");
        Assert.IsTrue(
            script.Contains("A lugger status must be Available, Backup, Full or Do Not Use.", StringComparison.Ordinal),
            "The refusal is reported in plain words.");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_RequiresAReasonForDoNotUseAndClearsItOtherwise()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains("NULLIF(TRIM(IFNULL(p_reason, '')), '')", StringComparison.Ordinal),
            "A reason of nothing but spaces is no reason (FR-019).");
        Assert.IsTrue(
            script.Contains("A reason is required when a lugger is marked Do Not Use.", StringComparison.Ordinal),
            "A Do Not Use without a reason is refused.");
        Assert.IsTrue(
            script.Contains("SET v_reason = NULL;", StringComparison.Ordinal),
            "Leaving Do Not Use clears the column while the change row keeps the reason (FR-019).");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_HoldsTheAssignmentRowsForTheWholeTransaction()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains("FROM waitlist_lugger_scrap_assignments AS a", StringComparison.Ordinal)
                && script.Contains("FOR UPDATE;", StringComparison.Ordinal),
            "The rows are held with SELECT ... FOR UPDATE, which is what makes two simultaneous edits one outcome.");

        var lockCount = script.Split("FOR UPDATE", StringSplitOptions.None).Length - 1;
        Assert.IsTrue(lockCount >= 2, $"Both the lugger row and the assignment rows are held; found {lockCount} lock(s).");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_PromotesTheFirstUsableLuggerWhenTheHolderCannotReceive()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains("SET is_usable = CASE WHEN status IN ('Available', 'Backup') THEN 1 ELSE 0 END;", StringComparison.Ordinal),
            "Only Available and Backup can receive material (FR-013).");
        Assert.IsTrue(
            script.Contains("SET v_next_holder_id = v_first_usable_id;", StringComparison.Ordinal),
            "A blocked holder hands the role to the first usable lugger in the type's order.");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_LeavesTheRoleWithTheHolderWhenTheEmptiedLuggerCouldTakeItBack()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains("IF v_current_holder_id IS NOT NULL AND v_current_holder_usable = 1 THEN", StringComparison.Ordinal),
            "An emptied lugger does not reclaim the role from a lugger that holds it (FR-016).");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_SortsEveryBlockedLuggerAfterEveryUsableOne()
    {
        var script = StatusSetScript;

        Assert.IsTrue(
            script.Contains("SET v_new_position = v_usable_count + v_blocked_seen;", StringComparison.Ordinal),
            "A blocked lugger sorts after every usable one, so it is never offered as the first choice (FR-017).");
    }

    [TestMethod]
    public void Service_LuggerTransitionStoreRuleTests_Engine_AppliesTheStatusAndRecordsEveryChangeInOneTransaction()
    {
        var script = StatusSetScript;

        Assert.IsTrue(script.Contains("START TRANSACTION;", StringComparison.Ordinal), "The whole change is one transaction.");
        Assert.IsTrue(script.Contains("SET status = v_status,", StringComparison.Ordinal), "The status is written once.");
        Assert.IsTrue(script.Contains("'status_changed'", StringComparison.Ordinal), "One change row per affected scrap type.");
        Assert.IsTrue(script.Contains("'dump_point_promoted'", StringComparison.Ordinal), "One row wherever the role moved.");
        Assert.IsTrue(
            script.Contains("INSERT INTO waitlist_lugger_arrangement_changes", StringComparison.Ordinal),
            "The change rows are written by the procedure, in the same transaction (FR-032).");
    }
}
