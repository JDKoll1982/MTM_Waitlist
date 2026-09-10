using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Live-DB validation of the DB-first Category/Item mapping (Phase 1.1): reads the real
/// <c>waitlist_request_types</c> / <c>waitlist_request_subtypes</c> catalog through
/// <see cref="RequestTypeCatalogService"/> and asserts every leaf row carries a canonical
/// <c>category</c> + <c>item_id</c>. Closes the "JSON Schema / DB-first re-scope" checklist box by
/// proving the DB read path surfaces the mapping against a live schema.
///
/// Requires MTM_WAITLIST_TEST_DB_CONNECTION_STRING to point at a schema-provisioned MySQL instance.
/// Without it the test reports inconclusive so the suite stays green offline.
/// </summary>
[TestClass]
public sealed class RequestTypeCatalogServiceIntegrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private RequestTypeCatalogService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping database integration tests.");
        }

        var helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));

        _service = new RequestTypeCatalogService(helper);
    }

    [TestMethod]
    public async Task LoadRequestTypes_Live_ReturnsCategoryItemMapping()
    {
        var types = await _service.LoadRequestTypesAsync();

        Assert.IsTrue(types.Count > 0, "The live catalog must return at least one request type.");

        // Every subtype leaf must carry a canonical category + item_id.
        var subtypes = types.SelectMany(t => t.Subtypes).ToList();
        Assert.IsTrue(subtypes.Count > 0, "The live catalog must return subtype leaves.");
        foreach (var subtype in subtypes)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(subtype.Category), $"Subtype '{subtype.Name}' must carry a canonical category.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(subtype.ItemId), $"Subtype '{subtype.Name}' must carry a canonical item_id.");
        }

        // Forklift Assist is the single type-leaf and must map to Other/other.
        var forklift = types.FirstOrDefault(t =>
            string.Equals(t.RequestType, "Forklift Assist", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(forklift, "Forklift Assist type-leaf should be present.");
        Assert.AreEqual("Other", forklift!.Category);
        Assert.AreEqual("other", forklift.ItemId);
    }

    [TestMethod]
    public async Task LoadRequestTypes_Live_CatalogIsReachable()
    {
        var types = await _service.LoadRequestTypesAsync();
        Assert.IsNotNull(types);
    }
}
