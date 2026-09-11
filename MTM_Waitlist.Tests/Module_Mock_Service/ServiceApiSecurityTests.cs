using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Api;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Service.Services;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Verifies the service API's security-critical behaviour at the operation seam: no secret ever reaches a
/// payload, an unknown shape or store is refused, and <b>no code path leads from the network surface to a
/// restore</b> (FR-013, FR-023, FR-026, SC-009, SC-010).
/// </summary>
/// <remarks>
/// Listener-level assertions (a real `401` with the error model, a real `404` for `/api/restore`) are part of
/// the end-to-end walkthrough rather than here, because they need a bound socket; these assertions cover the
/// behaviour that produces those responses.
/// </remarks>
[TestClass]
public sealed class ServiceApiSecurityTests
{
    [TestMethod]
    public async Task StatusPayload_CarriesNoCredentialOrConnectionSecret()
    {
        using var fixture = new ServiceApiFixture();

        var credentialPlaintext = await fixture.ConfigurationStore.GenerateCredentialAsync();
        var protectedBlob = Convert.ToBase64String(
            fixture.ConfigurationStore.Current.Api.Credential!.ProtectedValue);

        var outcome = await fixture.Operations.GetStatusAsync();

        Assert.IsTrue(outcome.Succeeded);

        var json = JsonSerializer.Serialize(
            outcome.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.IsFalse(json.Contains(credentialPlaintext, StringComparison.Ordinal), "The shared credential must never appear in a payload.");
        Assert.IsFalse(json.Contains(protectedBlob, StringComparison.Ordinal), "The protected credential blob must never appear in a payload.");
        Assert.IsFalse(json.Contains("password", StringComparison.OrdinalIgnoreCase), "No password-shaped field may appear in a payload.");
        Assert.IsTrue(json.Contains("\"credentialConfigured\":true", StringComparison.Ordinal), "The payload reports only that a credential exists.");

        Assert.IsTrue(outcome.Payload!.CredentialConfigured);
    }

    [TestMethod]
    public async Task Refresh_WithAnUnknownShape_IsRefusedAsUnknownShape()
    {
        using var fixture = new ServiceApiFixture();

        var outcome = await fixture.Operations.RunRefreshAsync(["not_a_shape"]);

        Assert.IsFalse(outcome.Succeeded);
        Assert.AreEqual(400, outcome.StatusCode);
        Assert.AreEqual("unknownShape", outcome.Error!.Error);
    }

    [TestMethod]
    public async Task Backup_AndListing_WithAnUnknownStore_AreRefused()
    {
        using var fixture = new ServiceApiFixture();

        var backupOutcome = await fixture.Operations.RunBackupAsync("not_a_store");
        var listOutcome = fixture.Operations.ListBackups("not_a_store");

        Assert.AreEqual(400, backupOutcome.StatusCode);
        Assert.AreEqual("unknownStore", backupOutcome.Error!.Error);
        Assert.AreEqual(400, listOutcome.StatusCode);
        Assert.AreEqual("unknownStore", listOutcome.Error!.Error);
    }

    [TestMethod]
    public void NoCodePathLeadsFromTheApiToARestore()
    {
        // FR-023: restore is host-only. The surface must not even depend on RestoreService, so no token —
        // valid or otherwise — can reach a destructive database replacement over the network.
        var apiMembers = typeof(ServiceApiOperations)
            .GetMembers()
            .Select(member => member.Name)
            .Concat(typeof(ServiceApiEndpoints).GetMembers().Select(member => member.Name))
            .ToList();

        Assert.IsFalse(
            apiMembers.Any(name => name.Contains("Restore", StringComparison.OrdinalIgnoreCase)),
            "The API surface must expose no restore operation at all.");

        var apiConstructorTypes = typeof(ServiceApiOperations)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.IsFalse(
            apiConstructorTypes.Contains(typeof(RestoreService)),
            "The API must not depend on RestoreService, so restore is unreachable from a request by construction.");
    }

    /// <summary>
    /// Builds a fully wired operations facade over real stores in a throwaway app-data folder.
    /// </summary>
    private sealed class ServiceApiFixture : IDisposable
    {
        private readonly string _appDataRoot;

        public ServiceApiFixture()
        {
            _appDataRoot = Path.Combine(Path.GetTempPath(), "mtm-mock-api-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_appDataRoot);

            ConfigurationStore = new ServiceConfigurationStore(_appDataRoot);
            ConfigurationStore.LoadAsync().GetAwaiter().GetResult();

            var configuration = ServiceConfiguration.CreateDefault(_appDataRoot);
            var catalogProvider = new RefreshShapeCatalogProvider(
                new AcceptingMetadataReader(),
                catalog: null,
                contentRoot: AppContext.BaseDirectory);
            catalogProvider.ValidateAsync().GetAwaiter().GetResult();

            RefreshRunRecords = new RefreshRunRecordStore(_appDataRoot);
            ArtifactStore = new BackupArtifactStore(_appDataRoot);

            var refreshEngine = new RefreshEngine(
                catalogProvider,
                new NoOpPayloadSource(),
                new NoOpMirrorWriter(),
                NullLogger<RefreshEngine>.Instance);

            var backupEngine = new BackupEngine(
                ArtifactStore,
                new MySqlConnectionStringResolver(configuration.MySqlConnection),
                () => ConfigurationStore.Current,
                NullLogger<BackupEngine>.Instance);

            Operations = new ServiceApiOperations(
                refreshEngine,
                catalogProvider,
                RefreshRunRecords,
                backupEngine,
                ArtifactStore,
                new EmptyFreshnessReader(),
                new UnreachableConnectivityProbe(),
                ConfigurationStore);
        }

        public ServiceConfigurationStore ConfigurationStore { get; }

        public RefreshRunRecordStore RefreshRunRecords { get; }

        public BackupArtifactStore ArtifactStore { get; }

        public ServiceApiOperations Operations { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_appDataRoot))
                {
                    Directory.Delete(_appDataRoot, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>Reports artifacts that satisfy the shipped catalog, including its population reads.</summary>
    private sealed class AcceptingMetadataReader : IVisualShapeMetadataReader
    {
        public Task<VisualShapeMetadata?> GetShapeMetadataAsync(
            string shapeKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<VisualShapeMetadata?>(Metadata(shapeKey));

        public Task<IReadOnlyList<VisualShapeMetadata>> GetAllShapeMetadataAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VisualShapeMetadata>>(
                VisualReadShapeCatalog.Create().Select(shape => Metadata(shape.Key)).ToList());

        private static VisualShapeMetadata Metadata(string shapeKey)
        {
            var shape = VisualReadShapeCatalog.FindByKey(shapeKey);

            if (shape is null)
            {
                return new VisualShapeMetadata(shapeKey, 0, 0, 0, 0, null);
            }

            var columns = new List<string> { "id" };
            columns.AddRange(shape.InputParameters.Select(parameter => ToSnakeCase(parameter.Name)));
            columns.AddRange(shape.OutputColumns.Select(column => ToSnakeCase(column.Name)));
            columns.Add("refreshed_utc");
            columns.Add("is_seed_content");

            return new VisualShapeMetadata(
                shapeKey,
                1,
                1,
                1,
                1,
                string.Join(",", columns.Distinct(StringComparer.OrdinalIgnoreCase)));
        }

        private static string ToSnakeCase(string value)
        {
            var builder = new System.Text.StringBuilder(value.Length + 8);

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];

                if (char.IsUpper(character))
                {
                    if (index > 0)
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                }
                else
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>Never produces a payload; the status read does not refresh anything.</summary>
    private sealed class NoOpPayloadSource : IVisualShapePayloadSource
    {
        public Task<string> BuildRefreshPayloadAsync(VisualReadShape shape, CancellationToken cancellationToken = default) =>
            Task.FromResult("[]");
    }

    /// <summary>Records nothing; the status read never writes to the mirror.</summary>
    private sealed class NoOpMirrorWriter : IMockMirrorRefreshWriter
    {
        public Task<int> RefreshAsync(VisualReadShape shape, string jsonPayload, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    /// <summary>Reports no freshness, which is the "cache not populated yet" state.</summary>
    private sealed class EmptyFreshnessReader : IVisualShapeFreshnessReader
    {
        public Task<IReadOnlyList<VisualShapeFreshness>> GetFreshnessAsync(
            string? shapeKey = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VisualShapeFreshness>>([]);
    }

    /// <summary>Reports the source as unreachable, exactly as an outage would.</summary>
    private sealed class UnreachableConnectivityProbe : IVisualConnectivityProbe
    {
        public Task<bool> ProbeAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
