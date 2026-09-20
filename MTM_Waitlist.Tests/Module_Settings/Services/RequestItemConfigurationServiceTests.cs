using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Tests.Module_Mock;

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

    /// <summary>The Item FR-035 names: it declares an enumerated answer and ships with no list of its own.</summary>
    private const string PickupComponent = "pickup-component";

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
            CataloguedItem,
            optionsJson: JsonSerializer.Serialize(new[] { "Alpha", "Beta", "Gamma" }),
            answerValueType: "enum"));

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Beta", "Gamma" },
            set.Get(CataloguedItem).Options.ToArray(),
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
    [DataRow("{\"Alpha\":true}", DisplayName = "options_json is not an array")]
    [DataRow("[\"Alpha\",\"\"]", DisplayName = "an option has no text")]
    [DataRow("[\"Alpha\",7]", DisplayName = "an option is not text")]
    public async Task GetConfigurationsAsync_MalformedOptionsJson_IsReportedAndNeverThrown(string payload)
    {
        var set = await ReadAsync(Row(CataloguedItem, optionsJson: payload, answerValueType: "enum"));

        var configuration = set.Get(CataloguedItem);

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

    // ── Raisability: an enumerated answer must have choices to draw on (FR-035) ─────────────────────────

    [TestMethod]
    public async Task GetConfigurationsAsync_EnumeratedAnswerWithNoConfiguredList_TakesItsChoicesFromTheJob()
    {
        // `pickup-component` is the shipped case: it declares an answer chosen from a list and is configured
        // with no list of its own, because its list arrives with the job snapshot. Its details step therefore
        // had nothing to draw on and the Item could not be raised at all (FR-035).
        var set = await ReadAsync(Row(
            PickupComponent,
            optionsJson: null,
            answerValueType: "enum",
            requiresAnswer: "1",
            detailFieldsJson: DetailFields(
                ("Component", "enum", "answer", 1, true),
                ("Part description", "string", "job", 2, false))));

        var configuration = set.Get(PickupComponent);
        Assert.IsTrue(configuration.IsAvailable, "The shipped row is a usable configuration, not a broken one.");

        var job = RequestJobPartAvailability.None with
        {
            HasActiveJob = true,
            HasComponent = true,
        };
        job = job.WithComponentPartNumbers(new[] { "CMP0004455", "CMP0004456" });

        var choices = RequestItemAnswerOptionsResolver.Resolve(configuration, job);

        Assert.IsTrue(
            choices.Count > 0,
            "Every in-scope Item whose configuration asks for an answer of a choice kind must yield at least one choice, so its details step can be completed — pickup-component is configured with no list of its own and is the Item this check names (FR-035).");
        CollectionAssert.AreEqual(
            new[] { "CMP0004455", "CMP0004456" },
            choices.ToArray(),
            "The choices come from the requesting job's component list and keep the job's order.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AConfiguredList_WinsOverTheJob()
    {
        // The same rule read the other way: a row that carries its own list uses it, so a fixed list is never
        // silently replaced by whatever the job happens to hold.
        var set = await ReadAsync(Row(
            CataloguedItem,
            optionsJson: JsonSerializer.Serialize(new[] { "Alpha", "Beta", "Gamma" }),
            answerValueType: "enum",
            requiresAnswer: "1",
            detailFieldsJson: DetailFields(("Choice", "enum", "answer", 1, true))));

        var job = RequestJobPartAvailability.None with { HasActiveJob = true, HasComponent = true };
        job = job.WithComponentPartNumbers(new[] { "CMP0004455" });

        CollectionAssert.AreEqual(
            new[] { "Alpha", "Beta", "Gamma" },
            RequestItemAnswerOptionsResolver.Resolve(set.Get(CataloguedItem), job).ToArray(),
            "A configured list is the list, whatever the job holds.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AnAnswerTheRowDoesNotDeclare_AsksForNothing_SoNoChoicesAreInvented()
    {
        var set = await ReadAsync(Row(CataloguedItem, answerValueType: "enum", requiresAnswer: "1"));

        var job = RequestJobPartAvailability.None with { HasActiveJob = true, HasComponent = true };
        job = job.WithComponentPartNumbers(new[] { "CMP0004455" });

        Assert.AreEqual(
            0,
            RequestItemAnswerOptionsResolver.Resolve(set.Get(CataloguedItem), job).Count,
            "A row that declares no enumerated answer field takes no list, from configuration or from the job (FR-013).");
    }

    [TestMethod]
    public void ShippedConfigurationSeed_PickupComponent_StillDeclaresAnEnumeratedAnswerWithNoListOfItsOwn()
    {
        // The fixture above is only a proof about the shipped data while the shipped row still looks like it.
        // This is the source-side half: the row is enumerated, asks for an answer, and carries no options_json.
        var seed = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Seeds",
            "seed_waitlist_request_item_configs",
            "create.sql"));

        var row = PickupComponentRow(seed);

        Assert.IsTrue(
            row.Contains(", 1, 'enum',", StringComparison.Ordinal),
            "pickup-component must still declare that it asks for an answer of a choice kind (FR-035).");
        Assert.IsFalse(
            row.Contains("JSON_ARRAY('", StringComparison.Ordinal),
            "pickup-component ships with no list of its own, which is why its choices must come from the job (FR-035).");
        Assert.IsTrue(
            row.Contains("'value_type','enum','source','answer'", StringComparison.Ordinal),
            "pickup-component must declare the enumerated answer field that consumes the choice.");
    }

    /// <summary>The shipped <c>pickup-component</c> INSERT row, from its Item code to the next row.</summary>
    private static string PickupComponentRow(string seedText)
    {
        var start = seedText.IndexOf("'" + PickupComponent + "'", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The shipped configuration seed no longer carries a row for {PickupComponent}.");

        var end = seedText.IndexOf("),\n\n--", start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = Math.Min(seedText.Length, start + 2000);
        }

        return seedText[start..end];
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

    // ── Which job list an enumerated answer names (FR-035, FR-050) ─────────────────────────────────────

    [TestMethod]
    public async Task GetConfigurationsAsync_AnAnswerNamingTheDunnageList_TakesTheJobsAssignedDunnageParts()
    {
        var set = await ReadAsync(Row(
            "pickup-dunnage",
            answerValueType: "enum",
            requiresAnswer: "1",
            detailFieldsJson: DetailFieldsWithList(("Dunnage part", "enum", "answer", "dunnage", 1, true))));

        var job = RequestJobPartAvailability.None with { HasActiveJob = true, HasDunnage = true };
        job = job.WithDunnageParts(new[]
        {
            new RequestDunnagePart { PartNumber = "DN-STL-4", DisplayName = "Steel Rack" },
            new RequestDunnagePart { PartNumber = "DN-BOX-2", DisplayName = "Boxes" },
        });

        CollectionAssert.AreEqual(
            new[] { "DN-STL-4", "DN-BOX-2" },
            RequestItemAnswerOptionsResolver.Resolve(set.Get("pickup-dunnage"), job).ToArray(),
            "A field naming the dunnage list takes the parts assigned to the job, in the job's order (FR-050).");
        Assert.AreEqual(
            RequestItemFieldDefinition.Lists.Dunnage,
            RequestItemAnswerOptionsResolver.DeclaredJobListName(set.Get("pickup-dunnage")),
            "The flow reads which step asks for the answer from the row, never from the Item's code (FR-013).");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AnAnswerNamingTheComponentList_TakesTheJobsComponents()
    {
        // The same mechanism read the other way: naming the list is what decides, so the two Items cannot be told
        // apart by anything but their rows (FR-013).
        var set = await ReadAsync(Row(
            CataloguedItem,
            answerValueType: "enum",
            requiresAnswer: "1",
            detailFieldsJson: DetailFieldsWithList(("Component", "enum", "answer", "component", 1, true))));

        var job = RequestJobPartAvailability.None with { HasActiveJob = true, HasComponent = true, HasDunnage = true };
        job = job
            .WithComponentPartNumbers(new[] { "CMP0004455" })
            .WithDunnageParts(new[] { new RequestDunnagePart { PartNumber = "DN-STL-4" } });

        CollectionAssert.AreEqual(
            new[] { "CMP0004455" },
            RequestItemAnswerOptionsResolver.Resolve(set.Get(CataloguedItem), job).ToArray(),
            "A field naming the component list takes the job's components, not the job's dunnage.");
    }

    [TestMethod]
    public async Task GetConfigurationsAsync_AListNameNothingSupplies_YieldsNoChoicesRatherThanAPlausibleList()
    {
        var set = await ReadAsync(Row(
            CataloguedItem,
            answerValueType: "enum",
            requiresAnswer: "1",
            detailFieldsJson: DetailFieldsWithList(("Something", "enum", "answer", "no-such-list", 1, true))));

        var job = RequestJobPartAvailability.None with { HasActiveJob = true, HasComponent = true, HasDunnage = true };
        job = job
            .WithComponentPartNumbers(new[] { "CMP0004455" })
            .WithDunnageParts(new[] { new RequestDunnagePart { PartNumber = "DN-STL-4" } });

        Assert.AreEqual(
            0,
            RequestItemAnswerOptionsResolver.Resolve(set.Get(CataloguedItem), job).Count,
            "A list name nothing supplies yields no choices, and the screen reports that rather than filling one in (FR-026, FR-035).");
    }

    [TestMethod]
    public void ShippedConfigurationSeed_BothDunnageRows_AskForTheJobsDunnageList()
    {
        // The mechanism above is only worth having while the shipped rows use it: both dunnage Items ask for an
        // answer, name the job's dunnage list, and carry no options_json of their own (FR-048, FR-050).
        var seed = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "Seeds",
            "seed_waitlist_request_item_configs",
            "create.sql"));

        foreach (var itemCode in new[] { "pickup-dunnage", "deliver-dunnage" })
        {
            var row = SeedRow(seed, itemCode);

            Assert.IsTrue(
                row.Contains(", 1, 'enum',", StringComparison.Ordinal),
                $"{itemCode} must declare that it asks for an answer of a choice kind (FR-048).");
            Assert.IsTrue(
                row.Contains("'value_type','enum','source','answer','list','dunnage'", StringComparison.Ordinal),
                $"{itemCode} must name the job's dunnage list on its answer field (FR-050).");
        }
    }

    // ── Fixtures ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The shipped INSERT row for one Item, from its Item code to the next row.</summary>
    private static string SeedRow(string seedText, string itemCode)
    {
        var start = seedText.IndexOf("'" + itemCode + "'", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"The shipped configuration seed no longer carries a row for {itemCode}.");

        var end = seedText.IndexOf("),\n\n--", start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = Math.Min(seedText.Length, start + 2000);
        }

        return seedText[start..end];
    }

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

    /// <summary>
    /// The same payload with the optional <c>list</c> key, which names the job-derived list a field's choices
    /// come from (FR-050).
    /// </summary>
    private static string DetailFieldsWithList(params (string Label, string ValueType, string Source, string? List, int Order, bool IsRequired)[] fields)
        => JsonSerializer.Serialize(fields.Select(field => new Dictionary<string, object?>
        {
            ["label"] = field.Label,
            ["value_type"] = field.ValueType,
            ["source"] = field.Source,
            ["list"] = field.List,
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
