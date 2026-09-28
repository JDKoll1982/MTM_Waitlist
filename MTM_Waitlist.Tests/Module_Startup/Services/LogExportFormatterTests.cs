using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The panel's copy: it carries the fault whole with its chain, the recorded context, the store's own diagnostics
/// and the entry's place in the chain, it names the filter it came from, it is cut with an explicit marker at a
/// stated ceiling, and a refused clipboard is reported rather than raised (FR-038, SC-017;
/// `contracts/logging-contract.md` §7.1).
/// </summary>
/// <remarks>
/// What is asserted is the text the clipboard is handed, because that text is what a reader pastes to somebody
/// who has no access to the store. The entry is written here as the store would have read it, so the test proves
/// the formatter passes values through rather than restating them.
/// </remarks>
[TestClass]
public sealed class LogExportFormatterTests
{
    /// <summary>The filter header the panel would produce, which the copy has to carry with it.</summary>
    private const string Filter = "window=last 30 day(s), severity=error, machine=MTMFG-161, page size=200";

    [TestMethod]
    public void Format_CarriesTheExceptionChainTheContextAndTheChainLink_ExactlyAsTheStoreHoldsThem()
    {
        // Arrange
        var entry = SampleEntry();

        // Act
        var text = LogExportFormatter.Format(Filter, [entry]);

        // Assert: the fault in full, so the copy is the evidence rather than a summary of it (FR-032, FR-038).
        StringAssert.Contains(text, entry.ExceptionDetail!, "the serialized exception chain was not carried");
        StringAssert.Contains(text, "System.InvalidOperationException", "the fault type was not carried");
        StringAssert.Contains(text, entry.Message, "the message was not carried");

        // The context the seam gathered, including the store's own diagnostics (FR-034 to FR-036).
        StringAssert.Contains(text, entry.PayloadJson!, "the payload was not carried verbatim");
        StringAssert.Contains(text, "\"errorCode\":\"1062\"", "the provider's error code was not carried");
        StringAssert.Contains(text, "sp_ops_startup_logs_insert", "the procedure was not carried");

        // The entry's place in the chain, which is the only part of it a reader verifies by eye (FR-021).
        StringAssert.Contains(text, "previous=" + entry.PreviousHash, "the previous link was not carried");
        StringAssert.Contains(text, "entry=" + entry.EntryHash, "the entry's own hash was not carried");

        // And who and what it happened to (FR-021).
        StringAssert.Contains(text, "MTMFG-161", "the machine was not carried");
        StringAssert.Contains(text, "ConfirmSignIn", "the action was not carried");
    }

    [TestMethod]
    public void Format_NamesTheFilterItCameFrom()
    {
        // Act
        var text = LogExportFormatter.Format(Filter, [SampleEntry()]);

        // Assert: a reader handed a pasted copy has to be able to tell a filtered sample from a whole history.
        StringAssert.Contains(text, "Filter: " + Filter);
    }

    [TestMethod]
    public void Format_WhenTheFilterIsBlank_StillSaysWhereTheCopyCameFrom()
    {
        // Act
        var text = LogExportFormatter.Format(null, [SampleEntry()]);

        // Assert: silence here would read as "everything", which is a claim the copy cannot make.
        StringAssert.Contains(text, "Filter: no filter");
    }

    [TestMethod]
    public void Format_WhenTheTextWouldExceedTheCeiling_IsCutWithAnExplicitMarkerThatNamesTheCeiling()
    {
        // Arrange: one entry whose message alone is several times the ceiling.
        var entry = SampleEntry() with { Message = new string('x', LogExportFormatter.CeilingCharacters * 3) };

        // Act
        var text = LogExportFormatter.Format(Filter, [entry]);

        // Assert: cut with a marker, never silently (FR-038).
        Assert.IsTrue(
            text.Length <= LogExportFormatter.CeilingCharacters,
            $"the copy was {text.Length} characters long, past its ceiling of {LogExportFormatter.CeilingCharacters}");
        StringAssert.Contains(text, LogExportFormatter.CutMarker, "the copy was cut without saying so");
        StringAssert.Contains(
            text,
            LogExportFormatter.CeilingCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "the marker does not name the ceiling it was cut at");
    }

    [TestMethod]
    public void Format_WhenTheTextIsShort_CarriesNoCutMarker()
    {
        // Act
        var text = LogExportFormatter.Format(Filter, [SampleEntry()]);

        // Assert: a short copy must not read as a cut one, exactly as a short exception chain must not.
        Assert.IsFalse(
            text.Contains(LogExportFormatter.CutMarker, StringComparison.Ordinal),
            "an uncut copy claimed to have been cut");
    }

    [TestMethod]
    public void Format_SeparatesTheEntries_AndSaysHowManyThereAre()
    {
        // Act
        var text = LogExportFormatter.Format(Filter, [SampleEntry(), SampleEntry() with { Id = 43 }]);

        // Assert: two entries are two entries, told apart without counting lines by eye.
        StringAssert.Contains(text, "Entries: 2");
        StringAssert.Contains(text, "Entry 1 of 2");
        StringAssert.Contains(text, "Entry 2 of 2");
        StringAssert.Contains(text, LogExportFormatter.EntrySeparator);
    }

    [TestMethod]
    public void Format_IntroducesNoNameTheStoreIsForbiddenToHold()
    {
        // Act
        var text = LogExportFormatter.Format(Filter, [SampleEntry()]);

        // Assert: the formatter adds no field of its own, so its own vocabulary cannot offer a place for a
        // credential or a token the store is forbidden to keep (§6, FR-036).
        foreach (var denied in new[] { "password", "token", "connectionstring", "salt", "pin", "credential", "key" })
        {
            Assert.IsFalse(
                text.Contains(denied + ":", StringComparison.OrdinalIgnoreCase),
                $"the copy's own labels offer a place for '{denied}'");
        }
    }

    [TestMethod]
    public void TheCeiling_IsTheNumberTheContractRecords()
    {
        // Arrange: the contract is where this task fixes the value, because no artifact stated it before.
        var contract = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "specs",
            "010-startup-rebuild",
            "contracts",
            "logging-contract.md");

        Assert.IsTrue(File.Exists(contract), $"the logging contract was not found at '{contract}'.");

        var text = File.ReadAllText(contract);
        var ceiling = LogExportFormatter.CeilingCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Assert: the cut is verifiable against a number rather than against a phrase.
        StringAssert.Contains(text, ceiling, "the contract does not record the ceiling the formatter cuts at");
    }

    [TestMethod]
    public void ACopyTheClipboardRefuses_IsReportedRatherThanRaised()
    {
        // Arrange: a clipboard that answers false, which is what the options overload does when the process is
        // not in the foreground, where Clipboard.SetContent would throw instead (§7.1).
        string? handedOver = null;
        var viewModel = new DeveloperLogPanelViewModel(
            new EmptyLogReader(),
            new AlwaysPermittingPermissionStub(),
            text =>
            {
                handedOver = text;
                return false;
            });

        viewModel.ApplyPermission(true);

        // Act
        viewModel.CopyEntryCommand.Execute(SampleEntry());

        // Assert: the refusal is a sentence on the panel, and the text the clipboard refused is the formatter's.
        Assert.AreEqual("Settings_LogPanel_Copy_Refused.Text".GetLocalized(), viewModel.MessageText);
        Assert.IsNotNull(handedOver, "nothing reached the clipboard at all");

        var refusedText = handedOver!;
        StringAssert.Contains(refusedText, "Chain link:", "the refused copy was not the formatter's text");
        StringAssert.Contains(refusedText, SampleEntry().EntryHash);
    }

    private static LogPanelEntry SampleEntry() => new()
    {
        Id = 42,
        PublicId = "11111111-1111-1111-1111-111111111111",
        CorrelationId = "22222222-2222-2222-2222-222222222222",
        CreatedUtc = new DateTime(2026, 9, 27, 9, 15, 0, DateTimeKind.Utc),
        Level = "error",
        EventAction = "ConfirmSignIn",
        Outcome = "Failure",
        ActorKind = "user",
        ActorId = "7",
        HostId = "MTMFG-161",
        MacAddress = "aa-bb-cc-dd-ee-ff",
        Module = "SignIn",
        ErrorType = "System.InvalidOperationException",
        Message = "The sign-in could not be confirmed because the store did not answer.",
        ExceptionDetail = """[{"level":0,"type":"System.InvalidOperationException","message":"the store did not answer","full":"System.InvalidOperationException: the store did not answer"}]""",
        ErrorFingerprint = new string('c', 64),
        PreviousHash = new string('a', 64),
        EntryHash = new string('b', 64),
        PayloadJson = """{"runtime":{"version":"1.2.3"},"ui":{"control":"SignInWindow/SignInButton"},"database":{"errorCode":"1062","sqlState":"23000","storedProcedure":"sp_ops_startup_logs_insert"}}""",
    };

    /// <summary>A store that answers every read with nothing, which is all this file needs of one.</summary>
    private sealed class EmptyLogReader : IMySqlHelperServer
    {
        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    /// <summary>Answers the one key the panel is gated on, and nothing else.</summary>
    private sealed class AlwaysPermittingPermissionStub : IPermissionService
    {
        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => true, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }
}
