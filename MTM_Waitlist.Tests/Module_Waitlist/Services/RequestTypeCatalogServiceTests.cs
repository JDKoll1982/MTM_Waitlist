using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

[TestClass]
public sealed class RequestTypeCatalogServiceTests
{
    [TestMethod]
    public async Task LoadRequestTypesAsync_MapsTypesAndSubtypes_FromDbRows()
    {
        var types = new[]
        {
            TypeRow(id: 1L, requestType: "Coil", control: "CoilControl", flow: "direct-to-confirmation", text: 0, null, 0, 200, null, null, null, "[\"Coil number\",\"Quantity in house\"]"),
            TypeRow(id: 2L, requestType: "Forklift Assist", control: "ForkliftControl", flow: "collect-input-then-confirm", text: 1, "Enter why", 5, 50, null, "Other", "other", "[\"Description\"]"),
        };
        var subtypes = new[]
        {
            SubtypeRow(requestTypeId: 1L, name: "Bring", control: "CoilSub", flow: "direct-to-confirmation", text: 0, null, 0, 200, null, "Deliver", "deliver-coil", "[\"Coil number\"]"),
            SubtypeRow(requestTypeId: 1L, name: "Pickup", control: "CoilSub", flow: "direct-to-confirmation", text: 0, null, 0, 200, null, "Pickup", "pickup-coil", "[\"Coil number\"]"),
        };
        var service = new RequestTypeCatalogService(new StubMySqlHelperServer(types, subtypes));

        var result = await service.LoadRequestTypesAsync();

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Coil", result[0].RequestType);
        Assert.AreEqual(2, result[0].Subtypes.Count);
        Assert.AreEqual("Bring", result[0].Subtypes[0].Name);
        Assert.AreEqual("Pickup", result[0].Subtypes[1].Name);
        Assert.AreEqual(2, result[0].CenterDataGridFields.Count);
        Assert.AreEqual("Coil number", result[0].CenterDataGridFields[0]);
        // Canonical leaf mapping columns (2026-09-08): type-level leaf (Forklift Assist) + each subtype.
        Assert.IsNull(result[0].Category);
        Assert.IsNull(result[0].ItemId);
        Assert.AreEqual("Other", result[1].Category);
        Assert.AreEqual("other", result[1].ItemId);
        Assert.AreEqual("Deliver", result[0].Subtypes[0].Category);
        Assert.AreEqual("deliver-coil", result[0].Subtypes[0].ItemId);
        Assert.AreEqual("Pickup", result[0].Subtypes[1].Category);
        Assert.AreEqual("pickup-coil", result[0].Subtypes[1].ItemId);
        Assert.IsTrue(result[1].RequiresTextInput);
        Assert.AreEqual("Enter why", result[1].PromptText);
        Assert.AreEqual(50, result[1].MaxLength);
        Assert.AreEqual(0, result[1].Subtypes.Count);
    }

    private static Dictionary<string, object?> TypeRow(
        long id, string requestType, string control, string flow, int text, string? prompt,
        int min, int max, string? image, string? category, string? itemId, string gridJson) => new()
    {
        ["id"] = id,
        ["public_id"] = Guid.NewGuid().ToString(),
        ["request_type"] = requestType,
        ["control"] = control,
        ["flow"] = flow,
        ["requires_text_input"] = (byte)text,
        ["prompt_text"] = prompt,
        ["min_length"] = min,
        ["max_length"] = max,
        ["default_image_path"] = image,
        ["category"] = category,
        ["item_id"] = itemId,
        ["center_data_grid_fields_json"] = gridJson,
        ["is_active"] = (byte)1,
    };

    private static Dictionary<string, object?> SubtypeRow(
        long requestTypeId, string name, string control, string flow, int text, string? prompt,
        int min, int max, string? image, string? category, string? itemId, string gridJson) => new()
    {
        ["id"] = 100L + requestTypeId,
        ["public_id"] = Guid.NewGuid().ToString(),
        ["request_type_id"] = requestTypeId,
        ["subtype_name"] = name,
        ["control"] = control,
        ["flow"] = flow,
        ["requires_text_input"] = (byte)text,
        ["prompt_text"] = prompt,
        ["min_length"] = min,
        ["max_length"] = max,
        ["default_image_path"] = image,
        ["category"] = category,
        ["item_id"] = itemId,
        ["center_data_grid_fields_json"] = gridJson,
        ["is_active"] = (byte)1,
    };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _typeRows;
        private readonly IReadOnlyList<Dictionary<string, object?>> _subtypeRows;

        public StubMySqlHelperServer(
            IReadOnlyList<Dictionary<string, object?>> typeRows,
            IReadOnlyList<Dictionary<string, object?>> subtypeRows)
        {
            _typeRows = typeRows;
            _subtypeRows = subtypeRows;
        }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            LastDatabaseTarget = databaseTarget;
            var rows = storedProcedureName.Contains("subtypes", StringComparison.OrdinalIgnoreCase) ? _subtypeRows : _typeRows;
            return Task.FromResult(rows);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            LastDatabaseTarget = databaseTarget;
            var rows = sql.Contains("waitlist_request_subtypes", StringComparison.OrdinalIgnoreCase) ? _subtypeRows : _typeRows;
            return Task.FromResult(rows);
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
