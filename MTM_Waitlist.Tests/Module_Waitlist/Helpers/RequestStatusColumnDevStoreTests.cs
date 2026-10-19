using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Converters;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;

using Windows.UI;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// Every state FR-011 to FR-023 names, read off the card row the <b>live store's own rows</b> produce: the list
/// procedure returns them, the service's own mapper turns them into requests, and
/// <see cref="WaitlistViewViewModel.CreateSessionOrder"/> builds the row the card binds (SC-006, SC-007).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RequestStatusColumnTests"/> pins the same states from rows written by hand. What this adds is the
/// seam in front of them: that the store's actual row shape — its status spellings, its timestamps, the employee
/// number it records a handler by, the name the list procedure resolves from the audit trail, and the close
/// instant it carries — is what the column ends up reading. A mapper, a procedure or a seed that drifted would
/// pass the hand-written test and fail this one.
/// </para>
/// <para>
/// <b>Read-only, and environment-gated.</b> It needs <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c> pointing at a
/// MySQL instance carrying the waitlist schema, its procedures and at least one row in each lifecycle state, and
/// reports inconclusive without one so the suite stays green offline. Nothing is written: the rows are the
/// store's own. A separate variable is used so the process-wide connection-string resolution the rest of the
/// suite relies on is not changed.
/// </para>
/// <para>
/// <b>The clock.</b> The seeded rows were raised before this ran, so the states that depend on time are read at a
/// stated instant rather than at "now": one minute after a request was raised for the live-in-its-window reading,
/// and five minutes past its due time for the overdue one. That is the same row the card draws, read at the two
/// moments its state changes — not a second row invented to stand for it.
/// </para>
/// </remarks>
[TestClass]
public sealed class RequestStatusColumnDevStoreTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    /// <summary>The parameter the card passes when it asks the converter for the column's theme brush.</summary>
    private const string ThemeParameter = RequestStatusToBrushConverter.StatusColumnThemeParameter;

    /// <summary>One of the four conditions, verbatim as <see cref="HandlerStatusCatalog"/> and the store hold it.</summary>
    private const string ReportedCondition = "Changing Propane";

    /// <summary>An employee number is digits only; a name is not, which is what the two checks below lean on.</summary>
    private static readonly Regex s_employeeNumberShape = new("^[0-9]+$", RegexOptions.CultureInvariant);

    private static readonly Regex s_timeLeftShape = new("^[0-9]{2}:[0-9]{2}$", RegexOptions.CultureInvariant);

    private static readonly Regex s_overrunShape = new("^-[0-9]{2}:[0-9]{2}$", RegexOptions.CultureInvariant);

    private MySqlHelperServer _helper = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive(
                $"{ConnectionStringVariable} is not set; skipping the live status-column read. "
                + "Set it to a waitlist store carrying at least one Pending, Accepted, Completed and Canceled request.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new WaitlistDatabaseOptions { ConnectionString = connectionString! }));
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_LiveUnclaimedRequest_ReadsWaitingWithTheRequesterInTheThirdRow()
    {
        var request = Row(await ReadStoreRowsAsync(), r => r.Status == "Pending" && !r.IsOverdue, "is an unclaimed request the store has not flagged overdue");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        row.RefreshTimeDerivedText(request.RequestedUtc.AddMinutes(1));

        Assert.IsFalse(row.IsOverdue, "A request read a minute after it was raised is already overdue, so its due time is not what the store carries (FR-014).");
        Assert.AreEqual("Waiting", row.StatusWord, "An unclaimed live request must read Waiting (FR-012).");
        Assert.AreEqual(request.RequesterEmployeeName, row.PersonText, "The third row must name the requester while nobody has claimed the request (FR-022).");
        Assert.IsTrue(row.HasPerson, "The requester's row was removed even though it has a value (FR-018).");
        Assert.IsFalse(row.HasRequester, "An open request repeats its requester in the fourth row (FR-022).");
        Assert.AreEqual(TimeRingMeasure.TimeLeft, row.RingMeasure, "A live request's ring measures the time left (FR-015).");
        StringAssert.Matches(row.RingText, s_timeLeftShape, "A live request's ring carries neither the time left nor the time taken (FR-015).");
        Assert.AreEqual(
            RequestStatusPalette.NormalColumnColor,
            ColumnColour(row.StatusTheme),
            "A Waiting column is not painted in the normal theme (FR-019).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_ClaimedRequest_RemovesThePersonRowRatherThanPrintingTheHandlersNumber()
    {
        var request = Row(await ReadStoreRowsAsync(), r => r.Status == "Accepted", "is a claimed request");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        row.RefreshTimeDerivedText(request.RequestedUtc.AddMinutes(1));

        Assert.AreEqual("In Progress", row.StatusWord, "A claimed request still inside its window must read the claiming state (FR-012).");
        Assert.IsNotNull(request.AssignedMaterialHandler, "The store row carries no handler, so this is not a claimed request to read from.");

        // The store records who holds a request by employee number and resolves their name from the entry they
        // themselves wrote. This row has no such entry, so the third row is removed and the number is never shown
        // in its place — an employee number is an identifier, not a name (FR-022).
        Assert.IsFalse(
            s_employeeNumberShape.IsMatch(row.PersonText),
            $"The person row prints the employee number '{row.PersonText}' where a person's name belongs (FR-022).");
        Assert.AreEqual(
            request.AssignedMaterialHandlerName ?? string.Empty,
            row.PersonText,
            "The third row names somebody other than the handler the store recorded (FR-022).");
        Assert.AreEqual(row.HasPerson, !string.IsNullOrWhiteSpace(request.AssignedMaterialHandlerName), "The person row is drawn when the store records no name for the handler, or removed when it does (FR-018).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_HandlerWhoseNameTheAuditRecords_IsNamedInTheThirdRow()
    {
        var request = Row(await ReadStoreRowsAsync(), r => !string.IsNullOrWhiteSpace(r.AssignedMaterialHandlerName), "records the name of the person holding it");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual(request.AssignedMaterialHandlerName, row.PersonText, "The third row must name the material handler the list procedure resolved (FR-022).");
        Assert.AreNotEqual(request.AssignedMaterialHandler, row.PersonText, "The third row falls back to the employee number when a name is recorded (FR-022).");
        Assert.IsFalse(s_employeeNumberShape.IsMatch(row.PersonText), "The third row prints a number where a person's name belongs (FR-022).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_OverdueRequestWithNoCondition_ReadsOverdueWithTheOverrunInTheRingOnTheOverdueTheme()
    {
        var request = Row(
            await ReadStoreRowsAsync(),
            r => r.Status == "Accepted" && r.HandlerStatus is null && r.TargetTimeUtc is not null,
            "is a live claim with a due time and no reported condition");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        row.RefreshTimeDerivedText(request.TargetTimeUtc!.Value.AddMinutes(5));

        Assert.IsTrue(row.IsOverdue, "The arrangement did not make the row overdue, so the reading below is not the overdue state (FR-014).");
        Assert.AreEqual("Overdue", row.StatusWord, "An overdue request with no condition must read the overdue marking (FR-012, FR-014).");
        Assert.AreEqual(StatusColumnTheme.Overdue, row.StatusTheme, "An overdue request did not take the overdue theme (FR-014).");
        Assert.AreEqual(
            RequestStatusPalette.OverdueColumnColor,
            ColumnColour(row.StatusTheme),
            "An overdue column is not painted in the overdue theme (FR-014).");
        StringAssert.Matches(row.RingText, s_overrunShape, "The ring does not carry the signed time overrun the settled layout puts there (FR-014, D29).");
        Assert.AreNotEqual(row.StatusWord, row.RingText, "The ring repeats the word on row 1 instead of measuring the overrun (FR-014).");
        Assert.IsTrue(row.RingValue > 1d, "The overdue row's ring does not carry the overrun (FR-014).");
        Assert.AreNotEqual(
            RequestStatusPalette.HandlerConditionColumnColor,
            ColumnColour(row.StatusTheme),
            "An overdue column is painted as a handler's condition, which is exactly the confusion SC-007 forbids (FR-023).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_CompletedRequest_FreezesTheRingOnTheTimeTakenWithTheHandlerThirdAndTheRequesterFourth()
    {
        var rows = await ReadStoreRowsAsync();
        var request = Row(
            rows,
            r => r.Status == "Completed" && r.CompletedUtc is not null && !string.IsNullOrWhiteSpace(r.AssignedMaterialHandlerName),
            "is a finished request whose handler the audit trail names");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual("Done", row.StatusBadgeText, "A finished request's lifecycle word is not Done (FR-012).");
        Assert.AreEqual(
            row.IsOverdue ? "Overdue" : "Done",
            row.StatusWord,
            "The word on row 1 does not follow FR-012's precedence: the overdue marking when the request overran, otherwise the lifecycle word.");
        Assert.AreEqual(request.CompletedUtc, row.ClosedUtc, "The close instant the store recorded is not the one the row froze on (FR-015).");
        Assert.AreEqual(TimeRingMeasure.TimeTaken, row.RingMeasure, "A closed request's ring must measure the time taken (FR-015).");
        Assert.AreEqual(
            SampleOrder.FormatTimeTaken(request.CompletedUtc, request.RequestedUtc),
            row.RingText,
            "A closed request's ring does not carry the time it took (FR-015).");
        Assert.AreEqual(
            SampleOrder.RingFraction(request.RequestedUtc, request.CompletedUtc, SampleOrder.DefaultAllottedMinutes, DateTimeOffset.UtcNow.AddHours(2)),
            row.RingValue,
            0.0001d,
            "A closed request's ring is still growing with the clock, so it is not frozen at the moment it closed (FR-015).");
        Assert.AreEqual(request.AssignedMaterialHandlerName, row.PersonText, "A finished request's third row must name the material handler (FR-022).");
        Assert.AreEqual(request.RequesterEmployeeName, row.RequesterText, "A finished request's fourth row must name the requester (FR-022).");
        Assert.IsTrue(row.HasRequester, "A finished request's requester row was removed even though it has a value (FR-018).");

        // The row above happens to hold a request its own requester handled, where the third and fourth rows read
        // the same name and the fourth could pass by repetition. The store's other finished shape has a handler
        // who is somebody else: the third row is removed for want of a name, and the fourth still names the
        // requester — which is what proves the fourth row reads the requester rather than the handler (FR-022).
        var unnamedHandler = Row(
            rows,
            r => r.Status == "Completed"
                && r.CompletedUtc is not null
                && string.IsNullOrWhiteSpace(r.AssignedMaterialHandlerName)
                && !string.IsNullOrWhiteSpace(r.AssignedMaterialHandler)
                && !string.Equals(r.AssignedMaterialHandler, r.RequesterEmployeeName, StringComparison.Ordinal),
            "is a finished request held by somebody other than its requester, whose name the store does not record");
        var unnamedRow = WaitlistViewViewModel.CreateSessionOrder(unnamedHandler);
        Assert.IsFalse(unnamedRow.HasPerson, "A finished request whose handler has no recorded name still draws a person row (FR-018, FR-022).");
        Assert.AreEqual(
            unnamedHandler.RequesterEmployeeName,
            unnamedRow.RequesterText,
            "A finished request's fourth row names the handler rather than the requester when the two are different people (FR-022).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_CancelledRequest_ReadsTheHandlerOrThePersonWhoCancelled()
    {
        var rows = await ReadStoreRowsAsync();
        var cancelled = rows.Where(r => r.Status == "Canceled").ToList();
        Assert.IsTrue(cancelled.Count > 0, "The store carries no withdrawn request, so there is nothing to read this state from.");

        var afterClaim = Row(cancelled, r => !string.IsNullOrWhiteSpace(r.AssignedMaterialHandlerName), "was withdrawn after it was claimed");
        var afterClaimRow = WaitlistViewViewModel.CreateSessionOrder(afterClaim);
        Assert.AreEqual(
            afterClaim.AssignedMaterialHandlerName,
            afterClaimRow.PersonText,
            "A withdrawn request that had a handler must name them (FR-022).");
        Assert.AreEqual(
            afterClaim.RequesterEmployeeName,
            afterClaimRow.RequesterText,
            "A withdrawn request must name its requester beneath the handler (FR-022).");
        Assert.IsTrue(afterClaimRow.HasRequester, "A withdrawn request's requester row was removed even though it has a value (FR-018).");

        var whileWaiting = Row(cancelled, r => string.IsNullOrWhiteSpace(r.AssignedMaterialHandler), "was withdrawn before anyone claimed it");
        var whileWaitingRow = WaitlistViewViewModel.CreateSessionOrder(whileWaiting);
        Assert.AreEqual(
            whileWaiting.RequesterEmployeeName,
            whileWaitingRow.PersonText,
            "A request withdrawn before anyone claimed it must name the person who ended it (FR-022).");
        Assert.IsFalse(
            s_employeeNumberShape.IsMatch(whileWaitingRow.PersonText),
            $"The person row prints the employee number '{whileWaitingRow.PersonText}' where a person's name belongs (FR-022).");
        Assert.AreNotEqual(
            RequestStatusPalette.HandlerConditionColumnColor,
            ColumnColour(whileWaitingRow.StatusTheme),
            "A withdrawn request is themed as a handler's condition, so the lighter red no longer means a condition (FR-023, SC-007).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_RequestCarryingACondition_ReadsOnlyThatConditionOnTheLighterRedTheme()
    {
        var request = Row(
            await ReadStoreRowsAsync(),
            r => r.Status == "Accepted" && r.HandlerStatus is null && r.TargetTimeUtc is not null,
            "is a live claim with a due time and no reported condition");
        var row = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.IsNull(request.HandlerStatus, "The store row already carries a condition, so what is read below is not the condition taking precedence.");
        row.RefreshTimeDerivedText(request.TargetTimeUtc!.Value.AddMinutes(5));
        Assert.IsTrue(row.IsOverdue, "The arrangement did not make the row overdue, so the precedence it checks is not being tested (FR-023).");

        // Applying the reported condition to the row is the in-memory half of SetHandlerStatusAsync, which
        // replaces the request it holds with one carrying the condition the store accepted (FR-072). The write
        // half is T058's and is pinned against a live store by WaitlistHandlerStatusRoundTripTests.
        Assert.IsTrue(HandlerStatusCatalog.IsCondition(ReportedCondition), "The condition this test reports is not one of the four the store accepts (FR-071).");
        row.HandlerStatus = ReportedCondition;

        Assert.AreEqual(ReportedCondition, row.StatusWord, "A reported condition must read verbatim and must hide the overdue marking (FR-012, FR-023).");
        Assert.AreEqual(StatusColumnTheme.HandlerCondition, row.StatusTheme, "A reported condition did not take the handler-condition theme (FR-072).");
        Assert.AreEqual(
            RequestStatusPalette.HandlerConditionColumnColor,
            ColumnColour(row.StatusTheme),
            "A condition's column is not painted in the lighter red (FR-072).");
        Assert.AreNotEqual(
            RequestStatusPalette.OverdueColumnColor,
            ColumnColour(row.StatusTheme),
            "A condition and the overdue marking are both shown, which FR-023 forbids (FR-023).");
    }

    [TestMethod]
    public async Task Database_RequestStatusColumnDevStoreTests_EveryStoredRow_RemovesAnEmptyPersonRowInsteadOfPrintingAnEmployeeNumber()
    {
        var rows = await ReadStoreRowsAsync();

        Assert.IsTrue(rows.Count > 0, "The store returned no rows, so this check would pass on nothing (SC-006).");

        foreach (var request in rows)
        {
            var row = WaitlistViewViewModel.CreateSessionOrder(request);

            Assert.AreEqual(
                !string.IsNullOrWhiteSpace(row.PersonText),
                row.HasPerson,
                $"The person row on request '{request.Id}' is blank but still drawn, or carries a value and is removed (FR-018).");
            Assert.AreEqual(
                !string.IsNullOrWhiteSpace(row.RequesterText),
                row.HasRequester,
                $"The requester row on request '{request.Id}' is blank but still drawn, or carries a value and is removed (FR-018).");
            Assert.IsFalse(
                row.HasPerson && s_employeeNumberShape.IsMatch(row.PersonText),
                $"The person row on request '{request.Id}' prints the employee number '{row.PersonText}' where a person's name belongs (FR-022).");
            Assert.IsFalse(
                row.HasRequester && s_employeeNumberShape.IsMatch(row.RequesterText),
                $"The requester row on request '{request.Id}' prints the employee number '{row.RequesterText}' where a person's name belongs (FR-022).");

            if (!row.IsClosed)
            {
                Assert.IsFalse(row.HasRequester, $"The open request '{request.Id}' repeats its requester in the fourth row (FR-022).");
            }

            Assert.IsFalse(
                row.StatusTheme == StatusColumnTheme.Overdue && ColumnColour(row.StatusTheme) == RequestStatusPalette.HandlerConditionColumnColor,
                $"Request '{request.Id}' is themed as overdue and as a condition at once, which FR-023 forbids.");
        }
    }

    /// <summary>
    /// Every row the shipped list procedure returns, resolved requests included, mapped by the service's own
    /// mapper so the shape the card reads is the shape the list reads.
    /// </summary>
    /// <remarks>
    /// <c>p_include_resolved</c> is 1 here because the card's closed states are only ever drawn from a resolved
    /// row; the application passes 0, so this is the one departure from the production call and it is stated
    /// rather than hidden. Everything else — the procedure, the connection, the mapper — is the application's own.
    /// </remarks>
    private async Task<IReadOnlyList<WaitlistRequest>> ReadStoreRowsAsync()
    {
        var rows = await _helper.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_request_list",
            new Dictionary<string, object?>
            {
                ["p_building"] = null,
                ["p_include_resolved"] = 1,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.IsTrue(
            rows.Count > 0,
            "sp_waitlist_request_list returned no rows, so the dev store carries no request to read a state from.");

        return rows
            .Select(WaitlistRequestService.MapRowToRequest)
            .OfType<WaitlistRequest>()
            .ToList();
    }

    /// <summary>
    /// The one row matching <paramref name="match"/>, failing with <paramref name="what"/> when the store carries
    /// none — a state the store cannot show is reported as missing rather than quietly skipped.
    /// </summary>
    private static WaitlistRequest Row(IReadOnlyList<WaitlistRequest> rows, Func<WaitlistRequest, bool> match, string what) =>
        rows.FirstOrDefault(match)
        ?? throw new AssertFailedException($"The dev store carries no request that {what}, so that state cannot be read from it.");

    /// <summary>
    /// The colour the card paints the whole column with, asked for exactly the way the card asks for it.
    /// </summary>
    /// <remarks>
    /// Read as a colour rather than through the converter's <c>Convert</c>, because finishing that conversion
    /// means building a <c>SolidColorBrush</c> and a WinUI brush cannot be built outside a UI thread.
    /// </remarks>
    private static Color ColumnColour(StatusColumnTheme theme) =>
        RequestStatusToBrushConverter.ColourFor(theme, ThemeParameter);
}
