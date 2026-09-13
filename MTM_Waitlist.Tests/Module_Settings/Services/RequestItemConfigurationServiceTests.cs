using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// US4 (FR-013, FR-014, FR-015, FR-026). What is proved here is the <b>mechanism</b>: the declared fields are
/// read from stored configuration, in the declared order, with the declared value types and labels — and both
/// directions of disagreement, plus a payload that cannot be read, are reported or ignored rather than
/// half-applied.
/// </summary>
/// <remarks>
/// No check in this class asserts today's field set. Every expectation is built from the payload the fixture
/// declares, because the field set is mutable by design: a test that hard-coded the shipped fields would turn
/// the next configuration change into a build failure (FR-015, §16.15).
/// </remarks>
[TestClass]
public sealed class RequestItemConfigurationServiceTests
{
    private const string CataloguedItem = "pickup-coil";

    private static readonly IRequestItemCatalogService Catalog = new RequestItemCatalogService();

    // ── The declared fields are data, read in declared order with their declared types and labels ───────

    [TestMethod]
    public async Task GetConfigurationsAsync_ReadsTheDeclaredFieldsInDeclaredOrder_WithTheirTypesAndLabels()
    {
        // Declared out of order on purpose: the stored `order` is what the page must follow.
        var set = await ReadAsync(Row(
            CataloguedItem,
            detailFieldsJson: DetailFields(
                ("Third declared", "enum", "answer", 3, true),
                ("First declared", "string", "job", 1, false),
                ("Second declared", "text", "fixed", 2, false))));

        var fields = set.Get(CataloguedItem).DetailFields;

        CollectionAssert.AreEqual(
            new[] { "First declared", "Second declared", "Third declared" },
            fields.Select(field => field.Label).ToArray(),
            "The page renders the fields in the order the configuration declares (FR-013).");
        CollectionAssert.AreEqual(
            new[] { RequestItemValueType.String, RequestItemValueType.Text, RequestItemValueType.Enum },
            fields.Select(field => field.ValueType).ToArray(),
            "Each field carries the declared value type.");
        CollectionAssert.AreEqual(
            new[] { RequestItemFieldDefinition.Sources.Job, RequestItemFieldDefinition.Sources.Fixed, RequestItemFieldDefinition.Sources.Answer },
            fields.Select(field => field.Source).ToArray(),
            "Each field carries the declared value source.");
        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            fields.Select(field => field.Order).ToArray(),
            "Each field keeps its declared position.");
        Assert.IsTrue(fields[2].IsRequired, "A declared required field must stay required.");
        Assert.IsFalse(fields[0].IsRequired, "A field the configuration does not mark required must not become required.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_ADifferentDeclaredFieldSet_IsReadInsteadOfTheOneBefore()
    {
        // The negative half of the mechanism claim: swap the payload and the read follows it, with no code
        // change anywhere. This is what keeps the next field edit a data edit (FR-015, SC-006).
        var first = await ReadAsync(Row(CataloguedItem, detailFieldsJson: DetailFields(("Alpha", "string", "job", 1, false))));
        var second = await ReadAsync(Row(CataloguedItem, detailFieldsJson: DetailFields(("Beta", "text", "job", 1, false), ("Gamma", "enum", "answer", 2, false))));

        CollectionAssert.AreEqual(
            new[] { "Alpha" },
            first.Get(CataloguedItem).DetailFields.Select(field => field.Label).ToArray(),
            "The first payload must be read as declared.");
        CollectionAssert.AreEqual(
            new[] { "Beta", "Gamma" },
            second.Get(CataloguedItem).DetailFields.Select(field => field.Label).ToArray(),
            "The second payload must replace the first entirely.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_OptionsJson_IsReadInDeclaredOrder()
    {
        var set = await ReadAsync(Row(
            "pickup-die",
            optionsJson: JsonSerializer.Serialize(new[] { "Die Shop", "Home Location", "Other" }),
            answerValueType: "enum"));

        CollectionAssert.AreEqual(
            new[] { "Die Shop", "Home Location", "Other" },
            set.Get("pickup-die").Options.ToArray(),
            "The options offered are the ones the configuration declares, in the order it declares them.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AnEmptyDeclaredFieldList_IsNoFieldsRatherThanAFault()
    {
        // An Item that asks nothing and shows nothing is a legitimate configuration, not a broken one.
        var set = await ReadAsync(Row(CataloguedItem, detailFieldsJson: "[]"));

        var configuration = set.Get(CataloguedItem);

        Assert.IsTrue(configuration.IsAvailable, "A row that declares no fields is still a usable configuration.");
        Assert.AreEqual(0, configuration.DetailFields.Count, "No declared fields means no fields, not a substituted set.");
    }

    // ── A payload that cannot be read is a plain-language configuration problem ─────────────────────────

    [DataTestMethod]
    [DataRow("{ not json", DisplayName = "detail_fields_json is not JSON at all")]
    [DataRow("\"just a string\"", DisplayName = "detail_fields_json is not an array")]
    [DataRow("[{\"label\":\"\",\"value_type\":\"string\",\"source\":\"job\",\"order\":1}]", DisplayName = "a declared field has no label")]
    [DataRow("[{\"label\":\"Part\",\"value_type\":\"integer\",\"source\":\"job\",\"order\":1}]", DisplayName = "a declared field names a value type that does not exist")]
    [DataRow("[{\"label\":\"Part\",\"source\":\"job\",\"order\":1}]", DisplayName = "a declared field has no value type")]
    [DataRow("[{\"label\":\"Part\",\"value_type\":\"string\",\"source\":\"nowhere\",\"order\":1}]", DisplayName = "a declared field names a source that does not exist")]
    [DataRow("[{\"label\":\"Part\",\"value_type\":\"string\",\"source\":\"job\"}]", DisplayName = "a declared field has no declared order")]
    [DataRow("[{\"label\":\"Part\",\"value_type\":\"string\",\"source\":\"job\",\"order\":0}]", DisplayName = "a declared field's order is out of range")]
    [DataRow("[{\"label\":\"Part\",\"value_type\":\"string\",\"source\":\"job\",\"order\":1},7]", DisplayName = "an entry in the field list is not an object")]
    public async Task GetConfigurationsAsync_MalformedDetailFieldsJson_IsReportedAndNeverThrown(string payload)
    {
        var set = await ReadAsync(Row(CataloguedItem, detailFieldsJson: payload));

        var configuration = set.Get(CataloguedItem);

        Assert.IsFalse(configuration.IsAvailable, "A payload that cannot be read must not be presented as a usable configuration.");
        Assert.AreEqual(
            RequestItemConfiguration.MalformedMessageKey,
            configuration.UnavailableMessageKey,
            "The report must come from the resource mechanism rather than being written into the service.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(configuration.UnavailableMessage),
            "A configuration problem must be reported in plain language, not left blank (FR-026).");
        Assert.AreNotEqual(
            RequestItemConfiguration.MalformedMessageKey,
            configuration.UnavailableMessage,
            "The person must be shown the sentence, never the bare resource key.");
        Assert.AreEqual(0, configuration.DetailFields.Count, "A payload that cannot be read must not be half-applied.");
    }

    [DataTestMethod]
    [DataRow("[unterminated", DisplayName = "options_json is not JSON at all")]
    [DataRow("{\"Die Shop\":true}", DisplayName = "options_json is not an array")]
    [DataRow("[\"Die Shop\",\"\"]", DisplayName = "an option has no text")]
    [DataRow("[\"Die Shop\",7]", DisplayName = "an option is not text")]
    public async Task GetConfigurationsAsync_MalformedOptionsJson_IsReportedAndNeverThrown(string payload)
    {
        var set = await ReadAsync(Row("pickup-die", optionsJson: payload, answerValueType: "enum"));

        var configuration = set.Get("pickup-die");

        Assert.IsFalse(configuration.IsAvailable, "A payload that cannot be read must not be presented as a usable configuration.");
        Assert.AreEqual(RequestItemConfiguration.MalformedMessageKey, configuration.UnavailableMessageKey, "The report must come through the resource mechanism.");
        Assert.AreEqual(0, configuration.Options.Count, "A payload that cannot be read must not be half-applied.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AnItemThatAsksForAnAnswerWithNoUsableAnswerType_IsReportedAsAConfigurationProblem()
    {
        // The wizard picks the input control from the declared answer type, so an Item that asks a question it
        // cannot express is a configuration fault, not something to render and hope (FR-013, FR-026).
        var set = await ReadAsync(Row(
            CataloguedItem,
            detailFieldsJson: DetailFields(("Part", "string", "job", 1, false)),
            answerValueType: "nonsense",
            requiresAnswer: "1"));

        var configuration = set.Get(CataloguedItem);

        Assert.IsFalse(configuration.IsAvailable, "An Item asking for an answer it cannot express is not usable as configured.");
        Assert.AreEqual(RequestItemConfiguration.MalformedMessageKey, configuration.UnavailableMessageKey, "The report must come through the resource mechanism.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AnItemThatAsksForNothing_IgnoresAnUnusableAnswerTypeItNeverUses()
    {
        var set = await ReadAsync(Row(
            CataloguedItem,
            detailFieldsJson: DetailFields(("Part", "string", "job", 1, false)),
            answerValueType: "nonsense"));

        Assert.IsTrue(set.Get(CataloguedItem).IsAvailable, "A value the Item never uses must not make it unavailable.");
    }

    // ── Both directions of disagreement ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetConfigurationsAsync_CataloguedItemWithNoRow_IsReportedUnavailableInPlainLanguage()
    {
        var set = await ReadAsync(Row(CataloguedItem, detailFieldsJson: DetailFields(("Part", "string", "job", 1, false))));

        var without = set.Get("pickup-scrap");

        Assert.IsFalse(without.IsAvailable, "An Item with no configuration row is unavailable, never half-configured (FR-014).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(without.UnavailableMessage), "The unavailable report must say something (FR-026).");
        Assert.IsFalse(set.Contains("pickup-scrap"), "An Item with no row is never offered as configured.");
        Assert.AreEqual(
            Catalog.GetAllItems().Count,
            set.All.Count,
            "Every catalogued Item appears in the read, so absence is a reported state rather than a missing entry.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_RowForAnItemThatIsNotCatalogued_IsIgnoredAndNeverOffered()
    {
        var set = await ReadAsync(
            Row(CataloguedItem, detailFieldsJson: DetailFields(("Part", "string", "job", 1, false))),
            Row("not-a-catalogued-item", detailFieldsJson: DetailFields(("Part", "string", "job", 1, false))));

        Assert.IsFalse(set.Contains("not-a-catalogued-item"), "A stray row is never offered (spec Edge Cases).");
        Assert.IsFalse(
            set.All.Any(configuration => string.Equals(configuration.Item, "not-a-catalogued-item", StringComparison.OrdinalIgnoreCase)),
            "A stray row must not appear in the read at all.");
        Assert.AreEqual(Catalog.GetAllItems().Count, set.All.Count, "A stray row must not inflate the configured set.");
        Assert.IsTrue(set.Get(CataloguedItem).IsAvailable, "One stray row must not disturb the rows that are valid.");
    }

    // ── Every operation goes through a stored procedure (FR-024) ────────────────────────────────────────

    [TestMethod]
    public async Task GetConfigurationsAsync_ReadsThroughItsOwnStoredProcedure()
    {
        var helper = Stub(Row(CataloguedItem, detailFieldsJson: DetailFields(("Part", "string", "job", 1, false))));

        await new RequestItemConfigurationService(helper, Catalog).GetConfigurationsAsync();

        Assert.AreEqual(1, helper.ExecutedQueries.Count, "The configuration is read once, not per row and not per keystroke (FR-013).");
        Assert.AreEqual(
            "sp_waitlist_request_item_configs_get",
            helper.ExecutedQueries[0].Sql,
            "The read must go through the stored procedure; no statement text is written in the application (FR-024).");
    }

    [TestMethod]
    public async Task GetConfigurationAsync_ForAnItemWithNoRow_AnswersUnavailableRatherThanNull()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();
        var service = new RequestItemConfigurationService(helper, Catalog);

        var configuration = await service.GetConfigurationAsync("pickup-scrap");

        Assert.IsNotNull(configuration, "Absence is answered with the unavailable report, never with null (FR-026).");
        Assert.IsFalse(configuration.IsAvailable, "The Item has no row, so it is unavailable.");
    }

    // ── Fixtures ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Builds a <c>detail_fields_json</c> payload out of the declared fields this test declares.</summary>
    private static string DetailFields(params (string Label, string ValueType, string Source, int Order, bool IsRequired)[] fields)
        => JsonSerializer.Serialize(fields.Select(field => new Dictionary<string, object?>
        {
            ["label"] = field.Label,
            ["value_type"] = field.ValueType,
            ["source"] = field.Source,
            ["order"] = field.Order,
            ["is_required"] = field.IsRequired,
        }));

    /// <summary>One row in the shape <c>sp_waitlist_request_item_configs_get</c> returns.</summary>
    private static Dictionary<string, object?> Row(
        string item,
        string? optionsJson = null,
        string? detailFieldsJson = null,
        string? answerValueType = null,
        string requiresAnswer = "0",
        int? allottedMinutes = null) => new()
        {
            ["public_id"] = Guid.NewGuid().ToString(),
            ["item"] = item,
            ["category"] = "Pickup",
            ["control_flow"] = "direct-to-confirmation",
            ["requires_answer"] = requiresAnswer,
            ["answer_value_type"] = answerValueType,
            ["prompt_text"] = null,
            ["min_length"] = 0,
            ["max_length"] = 200,
            ["options_json"] = optionsJson,
            ["detail_fields_json"] = detailFieldsJson,
            ["allotted_minutes"] = allottedMinutes,
        };

    private static async Task<RequestItemConfigurationSet> ReadAsync(params Dictionary<string, object?>[] rows)
        => await new RequestItemConfigurationService(Stub(rows), Catalog).GetConfigurationsAsync();

    /// <summary>A helper scripted with the rows the read will return, and nothing else.</summary>
    private static FakeMySqlHelperServer Stub(params Dictionary<string, object?>[] rows)
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(rows);
        return helper;
    }
}
