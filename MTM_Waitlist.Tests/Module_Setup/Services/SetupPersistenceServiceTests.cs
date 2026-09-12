using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;
using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class SetupPersistenceServiceTests
{
    [TestMethod]
    public async Task SaveAsync_WhenBackendWritesNoRows_ReturnsFailureAsync()
    {
        // "Writes no rows" is what the store reports when its procedure affects nothing. Driven by a stub so the
        // premise is deterministic and the test can never write to a live store (T155): the previous version
        // stood up the real helper server and relied on there being no store behind it.
        var activeJobCoordinator = new FakeActiveJobCoordinatorService(hasActiveJob: false);
        var mySqlHelperServer = new StubMySqlHelperServer(affectedRows: 0);
        var service = new SetupPersistenceService(activeJobCoordinator, mySqlHelperServer);

        var request = CreateRequest();
        var result = await service.SaveAsync(request, false);

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.RequiresReplacementConfirmation);
        Assert.IsTrue(result.Message.Contains("no rows", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(0, activeJobCoordinator.RegisterCalls);
        // The failure came from the store's own save procedure, on the application store (FR-001).
        CollectionAssert.Contains(mySqlHelperServer.NonQueryProcedures, "sp_setup_save_setup");
    }

    [TestMethod]
    public async Task SaveAsync_WhenActiveJobExistsWithoutForce_ReturnsReplacementPromptAsync()
    {
        var activeJobCoordinator = new FakeActiveJobCoordinatorService(hasActiveJob: true);
        var mySqlHelperServer = new StubMySqlHelperServer(affectedRows: 1);
        var service = new SetupPersistenceService(activeJobCoordinator, mySqlHelperServer);

        var request = CreateRequest();
        var result = await service.SaveAsync(request, false);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.RequiresReplacementConfirmation);
        Assert.AreEqual(0, activeJobCoordinator.RegisterCalls);
        // The prompt is asked before anything is written, so no store is touched at all.
        Assert.AreEqual(0, mySqlHelperServer.NonQueryProcedures.Count);
    }

    private static SetupSaveRequest CreateRequest()
    {
        return new SetupSaveRequest
        {
            WorkOrder = "WO-076951",
            PartNumber = "12345679",
            SequenceNumber = "20",
            WorkCenter = "Press 12"
        };
    }

    private sealed class FakeActiveJobCoordinatorService : IActiveJobCoordinatorService
    {
        private readonly bool _hasActiveJob;

        public FakeActiveJobCoordinatorService(bool hasActiveJob)
        {
            _hasActiveJob = hasActiveJob;
        }

        public int RegisterCalls { get; private set; }

        public Task<bool> HasActiveJobAsync(string workCenter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_hasActiveJob);
        }

        public Task RegisterActiveJobAsync(SetupSaveRequest request, CancellationToken cancellationToken = default)
        {
            RegisterCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private static readonly IReadOnlyList<Dictionary<string, object?>> s_noRows =
            Array.Empty<Dictionary<string, object?>>();

        private readonly int _affectedRows;

        public StubMySqlHelperServer(int affectedRows)
        {
            _affectedRows = affectedRows;
        }

        public List<string> NonQueryProcedures { get; } = new();

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(s_noRows);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            NonQueryProcedures.Add(storedProcedureName);
            return Task.FromResult(_affectedRows);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(s_noRows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private sealed class InMemoryLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _settings;

        public InMemoryLocalSettingsService(Dictionary<string, object> settings)
        {
            _settings = settings;
        }

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return Task.FromResult((T?)value);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _settings[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _settings.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _settings.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
