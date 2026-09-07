using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class RequestTypeEditorServiceTests
{
    [TestMethod]
    public async Task GetCatalogAsync_IncludesInactiveAndGroupsSubtypesByType()
    {
        var types = new[]
        {
            TypeRow(id: 1L, name: "Coil", isActive: 1),
            TypeRow(id: 2L, name: "Old Type", isActive: 0),
        };
        var subtypes = new[]
        {
            SubtypeRow(id: 10L, typeId: 1L, name: "Bring", isActive: 1),
            SubtypeRow(id: 11L, typeId: 1L, name: "Pickup", isActive: 0),
        };

        var stub = new StubMySqlHelperServer(types, subtypes);
        var service = new RequestTypeEditorService(stub);

        var catalog = await service.GetCatalogAsync();

        CollectionAssert.Contains(stub.QueryProcedures, "sp_waitlist_request_types_get_all");
        CollectionAssert.Contains(stub.QueryProcedures, "sp_waitlist_request_subtypes_get_all");
        Assert.AreEqual(2, catalog.Count);
        // Inactive type is still returned (editor must see it to re-activate).
        Assert.IsFalse(catalog[1].IsActive);
        Assert.AreEqual(2, catalog[0].Subtypes.Count);
        Assert.AreEqual("Pickup", catalog[0].Subtypes[1].Name);
        Assert.IsFalse(catalog[0].Subtypes[1].IsActive);
    }

    [TestMethod]
    public async Task AddSubtypeAsync_CallsSubtypesInsert_WithBusinessParams_NoId()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>(), Array.Empty<Dictionary<string, object?>>());
        var service = new RequestTypeEditorService(stub);

        var subtype = new RequestSubtypeEditorItem
        {
            Id = 0,
            RequestTypeId = 1L,
            Name = "New Sub",
            Control = "SomeControl",
            Flow = "direct-to-confirmation",
            RequiresTextInput = true,
            PromptText = "Why",
            MinLength = 2,
            MaxLength = 50,
            CenterDataGridFields = new List<string> { "FieldA" },
            IsActive = true,
        };

        var affected = await service.AddSubtypeAsync(subtype);

        Assert.IsTrue(affected > 0);
        Assert.AreEqual("sp_waitlist_request_subtypes_insert", stub.LastNonQueryProcedure);
        Assert.IsFalse(stub.LastNonQueryParameters!.ContainsKey("p_id"));
        Assert.AreEqual(1L, stub.LastNonQueryParameters["p_request_type_id"]);
        Assert.AreEqual("New Sub", stub.LastNonQueryParameters["p_subtype_name"]);
        Assert.AreEqual((byte)1, stub.LastNonQueryParameters["p_is_active"]);
        Assert.IsNotNull(stub.LastNonQueryParameters["p_center_data_grid_fields_json"]);
    }

    [TestMethod]
    public async Task UpdateSubtypeAsync_CallsSubtypesUpdate_WithId()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>(), Array.Empty<Dictionary<string, object?>>());
        var service = new RequestTypeEditorService(stub);

        var affected = await service.UpdateSubtypeAsync(new RequestSubtypeEditorItem { Id = 42L, RequestTypeId = 1L, Name = "Edited", IsActive = false });

        Assert.IsTrue(affected > 0);
        Assert.AreEqual("sp_waitlist_request_subtypes_update", stub.LastNonQueryProcedure);
        Assert.AreEqual(42L, stub.LastNonQueryParameters!["p_id"]);
        Assert.AreEqual((byte)0, stub.LastNonQueryParameters["p_is_active"]);
    }

    [TestMethod]
    public async Task DeleteSubtypeAsync_CallsSubtypesDelete_ById()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>(), Array.Empty<Dictionary<string, object?>>());
        var service = new RequestTypeEditorService(stub);

        var affected = await service.DeleteSubtypeAsync(7L);

        Assert.IsTrue(affected > 0);
        Assert.AreEqual("sp_waitlist_request_subtypes_delete", stub.LastNonQueryProcedure);
        Assert.AreEqual(7L, stub.LastNonQueryParameters!["p_id"]);
    }

    [TestMethod]
    public async Task UpdateTypeAsync_CallsTypesUpdate_WithAllFields()
    {
        var stub = new StubMySqlHelperServer(Array.Empty<Dictionary<string, object?>>(), Array.Empty<Dictionary<string, object?>>());
        var service = new RequestTypeEditorService(stub);

        var affected = await service.UpdateTypeAsync(new RequestTypeEditorItem
        {
            Id = 5L,
            Name = "Coil",
            Control = "CoilControl",
            Flow = "collect-input-then-confirm",
            RequiresTextInput = true,
            PromptText = "Enter coil",
            MinLength = 1,
            MaxLength = 20,
            IsActive = false,
            CenterDataGridFields = new List<string> { "Coil number", "Qty" },
        });

        Assert.IsTrue(affected > 0);
        Assert.AreEqual("sp_waitlist_request_types_update", stub.LastNonQueryProcedure);
        Assert.AreEqual(5L, stub.LastNonQueryParameters!["p_id"]);
        Assert.AreEqual("Coil", stub.LastNonQueryParameters["p_request_type"]);
        Assert.AreEqual((byte)0, stub.LastNonQueryParameters["p_is_active"]);
    }

    private static Dictionary<string, object?> TypeRow(long id, string name, int isActive) => new()
    {
        ["id"] = id,
        ["public_id"] = System.Guid.NewGuid().ToString(),
        ["request_type"] = name,
        ["control"] = "SomeControl",
        ["flow"] = "direct-to-confirmation",
        ["requires_text_input"] = (byte)0,
        ["prompt_text"] = null,
        ["min_length"] = 0,
        ["max_length"] = 200,
        ["default_image_path"] = null,
        ["center_data_grid_fields_json"] = null,
        ["is_active"] = (byte)isActive,
    };

    private static Dictionary<string, object?> SubtypeRow(long id, long typeId, string name, int isActive) => new()
    {
        ["id"] = id,
        ["public_id"] = System.Guid.NewGuid().ToString(),
        ["request_type_id"] = typeId,
        ["subtype_name"] = name,
        ["control"] = "SomeSubControl",
        ["flow"] = "direct-to-confirmation",
        ["requires_text_input"] = (byte)0,
        ["prompt_text"] = null,
        ["min_length"] = 0,
        ["max_length"] = 200,
        ["default_image_path"] = null,
        ["center_data_grid_fields_json"] = null,
        ["is_active"] = (byte)isActive,
    };

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _types;
        private readonly IReadOnlyList<Dictionary<string, object?>> _subtypes;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> types, IReadOnlyList<Dictionary<string, object?>> subtypes)
        {
            _types = types;
            _subtypes = subtypes;
        }

        public List<string> QueryProcedures { get; } = new();

        public string? LastNonQueryProcedure { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastNonQueryParameters { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryProcedures.Add(storedProcedureName);
            IReadOnlyList<Dictionary<string, object?>> rows = storedProcedureName.Contains("subtypes", StringComparison.Ordinal)
                ? _subtypes
                : _types;
            return Task.FromResult(rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            LastNonQueryProcedure = storedProcedureName;
            LastNonQueryParameters = new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
