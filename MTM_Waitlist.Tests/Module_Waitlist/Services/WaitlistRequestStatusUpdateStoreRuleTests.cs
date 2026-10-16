using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Giving a job back clears the condition its handler reported, and the store is where that has to be true —
/// not only the row the session happens to be holding.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is asserted against the artifact.</b> The rule lives in SQL, which no unit test can exercise
/// without a store, and the aggregate an installer applies is what a machine actually runs — the same reasoning
/// <see cref="HandlerNameStoreReadTests"/> records. A routine that was correct on disk and stale in the store is
/// the failure this repository has already shipped once, and its symptom here would be a card that shows the
/// previous handler's condition on a job nobody holds, on the first list read after the job was given back.
/// </para>
/// <para>
/// <b>What the clear is keyed on is the part worth protecting.</b> A single-table <c>UPDATE</c> evaluates its
/// <c>SET</c> list left to right, so an expression naming the <c>assigned_material_handler</c> column would read
/// the value this same statement had just written — empty for every release — and would clear the condition on
/// Accept and on Complete as well. Keying it on the status instead would clear the condition on an unrelated
/// transition, because a request is <c>Pending</c> while its note is written. So the assertions below hold the
/// rule to the incoming assignee parameter and to nothing else.
/// </para>
/// </remarks>
[TestClass]
public sealed class WaitlistRequestStatusUpdateStoreRuleTests
{
    /// <summary>The line that opens the clear, wherever it sits in the SET list.</summary>
    private const string ClearOpens = "handler_status = CASE";

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    private static string ProcedureText() => File.ReadAllText(
        Path.Combine(Root, "Database", "StoredProcedures", "sp_waitlist_request_status_update", "create.sql"));

    private static string AggregateText() => File.ReadAllText(
        Path.Combine(Root, "Database", "StoredProcedures", "AllSPs.sql"));

    /// <summary>
    /// The clear itself, cut out of the routine so an assertion cannot be satisfied by an unrelated line that
    /// happens to contain the same words.
    /// </summary>
    private static string ClearRuleIn(string routine)
    {
        var opens = routine.IndexOf(ClearOpens, StringComparison.Ordinal);
        Assert.IsTrue(
            opens >= 0,
            "sp_waitlist_request_status_update no longer clears handler_status, so a given-back job keeps reading the condition its previous handler reported (FR-106).");

        var closes = routine.IndexOf("END,", opens, StringComparison.Ordinal);
        Assert.IsTrue(closes > opens, "The handler_status CASE never closes, so the routine is not valid SQL.");

        return routine.Substring(opens, closes - opens);
    }

    [TestMethod]
    public void Database_WaitlistRequestStatusUpdateStoreRuleTests_Release_ClearsTheConditionWhenTheAssigneeGoes()
    {
        var rule = ClearRuleIn(ProcedureText());

        StringAssert.Contains(
            rule,
            "COALESCE(p_assigned_material_handler, '')",
            "The clear must read the incoming assignee parameter: that is the only value that says the job is being handed to nobody (FR-106).");
        StringAssert.Contains(
            rule,
            "THEN NULL",
            "An emptied assignee must clear the condition rather than keep it.");
        Assert.IsFalse(
            rule.Contains("ELSE assigned_material_handler", StringComparison.Ordinal),
            "Keeping the condition must not read the column this same statement has already rewritten.");
    }

    [TestMethod]
    public void Database_WaitlistRequestStatusUpdateStoreRuleTests_AcceptAndComplete_KeepTheConditionBecauseTheyCarryAnAssignee()
    {
        var rule = ClearRuleIn(ProcedureText());

        // The rule is keyed on the assignee, so every transition that carries one leaves the condition alone. Keyed
        // on the status instead, the same clear would fire for a request sitting in Pending — which is the state a
        // request is in while its note is written.
        Assert.IsFalse(
            rule.Contains("p_status", StringComparison.Ordinal),
            "The clear must not be keyed on the status: Accept and Complete keep a condition, and a note written to a Pending row must not silently remove one (FR-106).");
    }

    [TestMethod]
    public void Database_WaitlistRequestStatusUpdateStoreRuleTests_TheAggregateAnInstallerApplies_CarriesTheSameClear()
    {
        var rule = ClearRuleIn(AggregateText());

        StringAssert.Contains(
            rule,
            "COALESCE(p_assigned_material_handler, '')",
            "AllSPs.sql is what a machine applies. A clear that exists only in the per-artifact file never reaches an installed store, and the defect survives every test that reads the artifact.");
    }
}
