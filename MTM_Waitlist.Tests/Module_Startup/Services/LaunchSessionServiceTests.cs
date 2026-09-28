using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// FR-010, FR-011 and SC-011: session validity is judged by the store's clock and never by this computer's, a
/// sign-out clears the session before the application restarts, and a change to the session length takes effect
/// at the next sign-in.
/// </summary>
/// <remarks>
/// <para>
/// The double is a hand-written store that can hand back a session row and a session-length row. The clock case
/// works by deliberate contradiction: the row is valid according to the store and its expiry is long past
/// according to this computer, so a service that consulted the workstation's clock would report it invalid.
/// </para>
/// <para>
/// One case proves a read that never happens, which is how "the setting is read at issue time and not on every
/// check" is pinned rather than asserted.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchSessionServiceTests
{
    private const string SettingsReadProcedure = "sp_config_settings_values_get";
    private const string UpsertProcedure = "sp_auth_user_active_sessions_upsert";
    private const string GetProcedure = "sp_auth_user_active_sessions_get";
    private const string ClearProcedure = "sp_auth_user_active_sessions_clear";

    private const long UserId = 7L;
    private const long ComputerId = 3L;

    [TestMethod]
    public async Task ValidateAsync_WhenTheStoreSaysTheSessionNoLongerStands_ReportsItAsNotValid()
    {
        // Arrange
        var store = new StubSessionStore().WithSession(isValid: false);
        var service = new LaunchSessionService(store);

        await service.IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Act
        var state = await service.ValidateAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsTrue(state.HasStoredSession, "a row was stored, so this is a session that ended rather than one that never existed");
        Assert.IsFalse(state.IsValid, "the store's verdict is the answer (FR-010)");
    }

    [TestMethod]
    public async Task ValidateAsync_WhenTheExpiryIsPastOnThisComputerButLiveInTheStore_TrustsTheStore()
    {
        // Arrange: the store holds the session as live, and the expiry it computed is years before this
        // computer's idea of now. Comparing that expiry here would report the session as gone (FR-010).
        var store = new StubSessionStore()
            .WithSession(isValid: true, expiresUtc: new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var service = new LaunchSessionService(store);

        await service.IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Act
        var state = await service.ValidateAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsTrue(state.IsValid, "the store's clock decides, and the store said this session stands (FR-010)");
        Assert.AreEqual(new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc), state.ExpiresUtc);
    }

    [TestMethod]
    public async Task ValidateAsync_BeforeAnySessionWasIssued_ReportsNoStoredSessionWithoutReadingTheStore()
    {
        // Arrange
        var store = new StubSessionStore();
        var service = new LaunchSessionService(store);

        // Act
        var state = await service.ValidateAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsFalse(state.HasStoredSession);
        Assert.AreEqual(0, store.CallCount(GetProcedure), "there is no digest to present, so there is nothing to ask");
    }

    [TestMethod]
    public async Task IssueAsync_SendsTheSessionLengthTheSettingHolds()
    {
        // Arrange
        var store = new StubSessionStore { SessionLengthMinutes = 120 };

        // Act
        await new LaunchSessionService(store).IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.AreEqual(
            120L,
            Convert.ToInt64(store.LastParameters(UpsertProcedure)["p_session_length_minutes"], System.Globalization.CultureInfo.InvariantCulture),
            "the length in force is resolved at issue time and handed to the store (SC-011)");
    }

    [TestMethod]
    public async Task IssueAsync_WhenTheStoreHoldsNoOverride_UsesTheDeclaredEightHourDefault()
    {
        // Arrange: no session-length row at all, which is a store nobody has configured.
        var store = new StubSessionStore();

        // Act
        await new LaunchSessionService(store).IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.AreEqual(
            Convert.ToInt64(LaunchSessionService.DefaultSessionLengthMinutes),
            Convert.ToInt64(store.LastParameters(UpsertProcedure)["p_session_length_minutes"], System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task IssueAsync_AfterTheSessionLengthChanges_UsesTheNewValueAtTheNextSignIn()
    {
        // Arrange
        var store = new StubSessionStore { SessionLengthMinutes = 480 };
        var service = new LaunchSessionService(store);

        await service.IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Act: the setting changes, then the person signs in again.
        store.SessionLengthMinutes = 60;

        await service.IssueAsync(UserId, ComputerId, CancellationToken.None);

        // Assert: the second issue carries the new value, and the first was never revisited (SC-011).
        Assert.AreEqual(
            60L,
            Convert.ToInt64(store.LastParameters(UpsertProcedure)["p_session_length_minutes"], System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(2, store.CallCount(SettingsReadProcedure), "the setting is read once per issue rather than cached");
    }

    [TestMethod]
    public async Task ClearAsync_WhenASessionStands_EndsItAndForgetsTheIssuedDigest()
    {
        // Arrange
        var store = new StubSessionStore().WithSession(isValid: true);
        var service = new LaunchSessionService(store);

        await service.IssueAsync(UserId, ComputerId, CancellationToken.None);
        var readsBeforeClearing = store.CallCount(GetProcedure);

        // Act
        var ended = await service.ClearAsync(UserId, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsTrue(ended, "a live session was ended (FR-011)");
        Assert.AreEqual(1, store.CallCount(ClearProcedure));
        Assert.AreEqual(
            UserId,
            Convert.ToInt64(store.LastParameters(ClearProcedure)["p_user_id"], System.Globalization.CultureInfo.InvariantCulture));

        var afterClearing = await service.ValidateAsync(UserId, ComputerId, CancellationToken.None);

        Assert.IsFalse(afterClearing.HasStoredSession, "the digest was dropped, so nothing can be presented again (FR-011)");
        Assert.AreEqual(readsBeforeClearing, store.CallCount(GetProcedure), "a cleared session is not looked up again");
    }

    [TestMethod]
    public async Task ClearAsync_WhenTheStoreHoldsNothingToEnd_ReportsNothingEndedRatherThanAFailure()
    {
        // Arrange: the pair has no live row, which is what a second sign-out looks like.
        var store = new StubSessionStore { ClearAffectsNoRows = true };

        // Act
        var ended = await new LaunchSessionService(store).ClearAsync(UserId, ComputerId, CancellationToken.None);

        // Assert: signing out twice must not report a fault.
        Assert.IsFalse(ended);
        Assert.AreEqual(1, store.CallCount(ClearProcedure), "the store is still asked, because only it knows what stands");
    }

    [TestMethod]
    public async Task ClearAsync_WithoutAPersonOrAMachine_WritesNothingAtAll()
    {
        // Arrange
        var store = new StubSessionStore();

        // Act
        var ended = await new LaunchSessionService(store).ClearAsync(0, ComputerId, CancellationToken.None);

        // Assert
        Assert.IsFalse(ended);
        Assert.AreEqual(0, store.CallCount(ClearProcedure));
    }

    [TestMethod]
    public async Task IssueAsync_WithoutAPersonOrAMachine_RefusesToWriteASession()
    {
        // Arrange
        var store = new StubSessionStore();

        // Act / Assert: a session belongs to a person on a machine, so there is no row that could be written.
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => new LaunchSessionService(store).IssueAsync(0, ComputerId, CancellationToken.None));

        Assert.AreEqual(0, store.CallCount(UpsertProcedure));
    }

    [TestMethod]
    public async Task IssueAsync_WhenTheStoreAcceptsNoRow_ReportsThatNoSessionWasIssued()
    {
        // Arrange: the store answers the write with zero affected rows, which is a write that did not land.
        var store = new StubSessionStore { UpsertAffectsNoRows = true };

        // Act / Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => new LaunchSessionService(store).IssueAsync(UserId, ComputerId, CancellationToken.None));
    }

    /// <summary>
    /// A store that can answer the session reads and writes and the session-length read, recording what it was
    /// asked so a claim about a call can be read back.
    /// </summary>
    private sealed class StubSessionStore : IMySqlHelperServer
    {
        private readonly List<(string Procedure, IReadOnlyDictionary<string, object?> Parameters)> _calls = [];

        private Dictionary<string, object?>? _sessionRow;

        public int? SessionLengthMinutes { get; set; } = LaunchSessionService.DefaultSessionLengthMinutes;

        public bool UpsertAffectsNoRows { get; init; }

        public bool ClearAffectsNoRows { get; init; }

        public StubSessionStore WithSession(bool isValid, DateTime? expiresUtc = null)
        {
            _sessionRow = new Dictionary<string, object?>
            {
                ["public_id"] = "11111111-2222-3333-4444-555555555555",
                ["issued_utc"] = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                ["expires_utc"] = expiresUtc ?? new DateTime(2026, 1, 1, 16, 0, 0, DateTimeKind.Utc),
                ["source_label"] = LaunchSessionService.SessionSourceLabel,
                ["is_valid"] = isValid ? 1L : 0L,
            };

            return this;
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            if (string.Equals(storedProcedureName, GetProcedure, StringComparison.Ordinal))
            {
                return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                    _sessionRow is null ? [] : [_sessionRow]);
            }

            if (string.Equals(storedProcedureName, SettingsReadProcedure, StringComparison.Ordinal))
            {
                return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(
                    SessionLengthMinutes is null
                        ? []
                        : [new Dictionary<string, object?> { ["setting_value_int"] = (long)SessionLengthMinutes.Value }]);
            }

            return Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>([]);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Record(storedProcedureName, parameters);

            if (string.Equals(storedProcedureName, UpsertProcedure, StringComparison.Ordinal) && UpsertAffectsNoRows)
            {
                return Task.FromResult(0);
            }

            if (string.Equals(storedProcedureName, ClearProcedure, StringComparison.Ordinal) && ClearAffectsNoRows)
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the session path (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written in the session path (constitution III).");

        public int CallCount(string storedProcedureName)
            => _calls.Count(call => string.Equals(call.Procedure, storedProcedureName, StringComparison.Ordinal));

        public IReadOnlyDictionary<string, object?> LastParameters(string storedProcedureName)
            => _calls.Last(call => string.Equals(call.Procedure, storedProcedureName, StringComparison.Ordinal)).Parameters;

        private void Record(string storedProcedureName, IReadOnlyDictionary<string, object?> parameters)
            => _calls.Add((storedProcedureName, parameters));
    }
}
