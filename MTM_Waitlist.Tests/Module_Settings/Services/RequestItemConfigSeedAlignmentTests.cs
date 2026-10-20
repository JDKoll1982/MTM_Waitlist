using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The seed and the card agree on every one of the twenty-four Items: the label the card shows is the label the
/// shipped configuration declares, and every field the reconciliation leaves declared is still declared
/// (FR-029, FR-056, FR-059, FR-095, FR-103).
/// </summary>
/// <remarks>
/// <para>
/// The expected side of every assertion is the settlement the specification wrote down —
/// <c>data-model.md</c> §9's per-Item reconciliation and <c>docs/research/research-waitlist-card-layout-pre-spec.md</c> §5's cell table —
/// and the actual side is <b>read from the shipped seed and from the shipped card</b>: the seed's
/// <c>create.sql</c> is parsed here, and the card is driven through
/// <see cref="WaitlistViewViewModel.CreateSessionOrder"/> with the declarations that parse produced. So this is
/// not a copy of the seed checked against itself: a declaration that drifts away from what the card reads fails
/// here, which is what makes the seed change (D33, T080) verifiable rather than merely reviewed.
/// </para>
/// <para>
/// <b>What "declared" means.</b> An Item's cells are a subset of its declared fields (§9): the four collapsing
/// Items draw nothing, the work-centre fields are never cells (FR-005, D19), and the four returning pickups draw
/// only the derived type and the skid count (FR-056). The fields the card does not draw are <b>left declared</b>
/// rather than removed, so this suite asserts both directions: nothing the card needs is missing, and nothing the
/// card excludes has been quietly deleted from configuration.
/// </para>
/// </remarks>
[TestClass]
public sealed class RequestItemConfigSeedAlignmentTests
{
    /// <summary>
    /// The labels the card requires each Item to <b>declare</b>, from <c>data-model.md</c> §9's "Cells the card
    /// requires" column and <c>docs/research/research-waitlist-card-layout-pre-spec.md</c> §5's cell table. The two die Items' third cell
    /// is the label-less die-shop phrase, so what they must declare is its answer's own label rather than the
    /// phrase — named through <see cref="WaitlistCardCellRules.DieShopAnswerLabel"/> so the seed and the card
    /// cannot spell it two different ways.
    /// </summary>
    private static readonly Dictionary<string, string[]> s_cellsTheCardRequires = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight"],
        ["pickup-die"] = ["Die", "Die Location", WaitlistCardCellRules.DieShopAnswerLabel],
        ["pickup-component"] = ["Component", "Total Qty In House", "Item Type"],
        ["pickup-fg"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-ncm"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-wip"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-outside-service"] = ["Part", "Part Type", "# of Skids"],
        ["pickup-riser-table"] = [],
        ["pickup-dunnage"] = ["Dunnage part", "Home Location"],
        ["pickup-scrap"] = ["Scrap Type", "Lugger Location"],
        ["pickup-hopper"] = ["Size"],
        ["deliver-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight"],
        ["deliver-flatstock"] = ["Part", "Quantity", "Destination"],
        ["deliver-component"] = ["Part description", "Total Qty In House", "Item Type"],
        ["deliver-die"] = ["Die", "Die Location", WaitlistCardCellRules.DieShopAnswerLabel],
        ["deliver-dunnage"] = ["Dunnage part", "Home Location"],
        ["deliver-wrong-coil"] = ["Coil number", "Correct coil"],
        ["deliver-wrong-flatstock"] = ["Part", "Correct flatstock"],
        ["deliver-riser-table"] = [],
        ["deliver-hopper"] = ["Size"],
        ["assist-coil-turn"] = ["Coil Number", "Weight of Coil"],
        ["assist-table-place"] = [],
        ["assist-table-remove"] = [],
        ["other"] = ["Request description"],
    };

    /// <summary>
    /// The labels each Item's declaration carries <b>today</b>, from <c>data-model.md</c> §9's "Seed declares
    /// today" column. Every one of them is still declared after the reconciliation, either under the spelling
    /// listed here or under the corrected spelling <see cref="s_movedLabels"/> records — a declaration the card
    /// excludes is a card rule, never a configuration edit (FR-056, FR-095).
    /// </summary>
    private static readonly Dictionary<string, string[]> s_declaredToday = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight", "Requesting work center"],
        ["pickup-die"] = ["Die", "Pickup location", "Quantity"],
        ["pickup-component"] = ["Component", "Part description", "Quantity", "Requesting work center"],
        ["pickup-fg"] = ["Part number", "Quantity remaining", "Part description", "Customer", "Packlist"],
        ["pickup-ncm"] = ["Part", "Quantity to move", "Pickup location", "Destination", "Traceability ID"],
        ["pickup-wip"] = ["Part and quantity", "Pickup work center", "WIP destination", "Operation sequence"],
        ["pickup-outside-service"] = ["Part", "Quantity to move", "Pickup location", "Destination", "Traceability ID"],
        ["pickup-riser-table"] = ["Requesting work center"],
        ["pickup-dunnage"] = ["Dunnage part", "Description", "Home location", "Quantity on hand"],
        ["pickup-scrap"] = ["Scrap type", "Quantity involved", "Scrap lugger", "Pickup work center"],
        ["pickup-hopper"] = ["Pickup work center"],
        ["deliver-coil"] = ["Coil number", "Quantity in house", "Coil description", "Average coil weight", "Requesting work center"],
        ["deliver-flatstock"] = ["Part", "Quantity", "Work center", "Destination"],
        ["deliver-component"] = ["Component", "Part description", "Quantity", "Requesting work center"],
        ["deliver-die"] = ["Die", "Pickup location", "Quantity"],
        ["deliver-dunnage"] = ["Dunnage part", "Description", "Home location", "Quantity on hand"],
        ["deliver-wrong-coil"] = ["Coil number", "Reason", "Correct coil"],
        ["deliver-wrong-flatstock"] = ["Part", "Reason", "Correct flatstock"],
        ["deliver-riser-table"] = ["Requesting work center"],
        ["deliver-hopper"] = ["Requesting work center"],
        ["assist-coil-turn"] = ["Coil number", "Quantity in house"],
        ["assist-table-place"] = ["Part", "Quantity", "Work center", "Destination"],
        ["assist-table-remove"] = ["Part", "Quantity", "Work center", "Destination"],
        ["other"] = ["Request description", "Requested work center"],
    };

    /// <summary>
    /// The labels this reconciliation moves, as the Item's declaration spells the field today and the label the
    /// card reads it under afterwards (D33, FR-059). Both are a cell of that Item while the move is in flight, so
    /// a store that has not run the migration still renders a label rather than an empty cell.
    /// </summary>
    private static readonly Dictionary<string, (string From, string To)[]> s_movedLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pickup-die"] = [("Pickup location", "Die Location")],
        ["deliver-die"] = [("Pickup location", "Die Location")],
        ["pickup-dunnage"] = [("Home location", "Home Location")],
        ["deliver-dunnage"] = [("Home location", "Home Location")],
        ["pickup-scrap"] = [("Scrap type", "Scrap Type")],
        ["assist-coil-turn"] = [("Coil number", "Coil Number")],
        ["pickup-fg"] = [("Part number", "Part")],
        ["pickup-wip"] = [("Part and quantity", "Part")],
    };

    /// <summary>
    /// The labels the card draws today whose value no source supplies yet (FR-058, D41,
    /// <c>docs/research/research-waitlist-card-layout-pre-spec.md</c> §5.2): the quantity cells wait on D38's read mechanism.
    /// They stay declared so the cell appears the day the source lands, with no card change (FR-034).
    /// </summary>
    /// <remarks>
    /// The quantity cells are hidden until their read lands. <c>Lugger Location</c> is no longer one of them: the
    /// requesting job's scrap type resolves it through the lugger arrangement, so the card draws it with a real
    /// value (FR-022, FR-026).
    /// </remarks>
    private static readonly string[] s_labelsWithNoSourceYet =
    [
        "Quantity in house",
        "Total Qty In House",
        "Quantity",
    ];

    /// <summary>
    /// The declared labels the card draws from a value the requesting job supplies rather than from an answer, so
    /// they are drawn only when the job carries one. They are declared like any other cell, so a job that does carry
    /// the value draws it with no configuration change (FR-022, FR-034).
    /// </summary>
    private static readonly string[] s_labelsWaitingOnTheirSource =
    [
        "Lugger Location",
    ];

    /// <summary>
    /// The declaration that produces a cell with <b>no label</b> — the die Items' die-shop direction, whose phrase
    /// IS its value (FR-055, D40). Its label is asserted where the phrase is, so it is not one of the labels this
    /// suite compares; that it is declared, and that the card reads the die-shop cell from it, is asserted here.
    /// </summary>
    private static readonly string s_labelLessDeclaration = WaitlistCardCellRules.DieShopAnswerLabel;

    // ── The seed declares what the card reads ────────────────────────────────────────────────────────────

    /// <summary>
    /// Every label the card requires is declared by the shipped seed, for all twenty-four Items, so a label the
    /// card shows is a label the configuration carries rather than one the card invents (FR-029, FR-059).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_DeclaresEveryLabelTheCardRequires()
    {
        var declarations = SeedDeclarations();
        var missing = new List<string>();

        AssertSettledTableCoversEveryItem();

        foreach (var (itemCode, labels) in s_cellsTheCardRequires)
        {
            var declared = DeclaredLabels(declarations, itemCode);

            foreach (var label in labels)
            {
                if (!declared.Contains(label, StringComparer.Ordinal))
                {
                    missing.Add($"`{itemCode}` requires a `{label}` cell and the seed declares no field under that label.");
                }
            }
        }

        Assert.AreEqual(
            0,
            missing.Count,
            "The seed and the card disagree, so the card would draw a cell whose label configuration does not carry "
            + "(FR-029, FR-059):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// The card draws nothing the settlement does not name, Item by Item: a drawn label is one of that Item's
    /// cells, and each Item whose sources all exist draws exactly the cells it declares (FR-025, FR-026, FR-056).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Card_DrawsTheDeclaredCellsAndNothingElse()
    {
        var declarations = SeedDeclarations();
        var unexpected = new List<string>();

        foreach (var (itemCode, labels) in s_cellsTheCardRequires)
        {
            var drawn = DrawnLabels(declarations, itemCode);

            foreach (var cell in drawn.Where(cell => !string.IsNullOrEmpty(cell)))
            {
                if (!labels.Contains(cell, StringComparer.Ordinal))
                {
                    unexpected.Add(
                        $"`{itemCode}` draws a `{cell}` cell, which is not one of its settled cells "
                        + $"({string.Join(", ", labels)}).");
                }
            }

            var expected = labels
                .Where(label => !s_labelsWithNoSourceYet.Contains(label, StringComparer.OrdinalIgnoreCase))
                .Where(label => !string.Equals(label, s_labelLessDeclaration, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            CollectionAssert.AreEqual(
                expected,
                drawn.ToArray(),
                $"`{itemCode}` draws {Describe(drawn)} where the settlement requires {Describe(expected)} (FR-026, FR-059).");
        }

        Assert.AreEqual(
            0,
            unexpected.Count,
            "The card draws a cell the settlement does not name (FR-056):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, unexpected));

        // A cell whose value the job supplies is drawn only when the job carries one, and this job carries the
        // lugger the arrangement named for its scrap type, so the cell appears with a real value (FR-022, FR-026).
        foreach (var (itemCode, labels) in s_cellsTheCardRequires)
        {
            var drawn = DrawnLabels(declarations, itemCode);

            foreach (var waiting in labels.Where(label => s_labelsWaitingOnTheirSource.Contains(label, StringComparer.OrdinalIgnoreCase)))
            {
                CollectionAssert.Contains(
                    drawn.ToArray(),
                    waiting,
                    $"`{itemCode}` does not draw its `{waiting}` cell, so a value the job supplies never reaches the card (FR-022, FR-026).");
            }
        }
    }

    // ── Every declared field the card does not draw is left declared ─────────────────────────────────────

    /// <summary>
    /// Nothing is removed from the declarations: every field §9 records as declared today is still declared
    /// afterwards, under its own spelling or the corrected one, including the dunnage <c>Description</c> whose
    /// removal is the open question (FR-056, FR-095, T083).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_LeavesEveryExcludedFieldDeclared()
    {
        var declarations = SeedDeclarations();
        var dropped = new List<string>();

        foreach (var (itemCode, labels) in s_declaredToday)
        {
            var declared = DeclaredLabels(declarations, itemCode);

            foreach (var label in labels)
            {
                var corrected = CorrectedLabel(itemCode, label);

                if (!declared.Contains(label, StringComparer.Ordinal)
                    && !declared.Contains(corrected, StringComparer.Ordinal))
                {
                    dropped.Add($"`{itemCode}` no longer declares `{label}`, which the card excludes rather than the seed removing (FR-056).");
                }
            }
        }

        Assert.AreEqual(
            0,
            dropped.Count,
            "The seed dropped a declaration the card excludes, which makes the exclusion a configuration edit rather "
            + "than a card rule (FR-056, FR-095):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, dropped));
    }

    /// <summary>
    /// The dunnage <c>Description</c> declaration is left in place and the card never draws it (FR-095, T083). The
    /// removal question stays open with <c>clarify</c>: this test records today's answer — keep it declared, never
    /// draw it — rather than deciding it.
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Card_NeverDrawsTheDunnageDescription()
    {
        var declarations = SeedDeclarations();
        var dunnageItems = new[] { "pickup-dunnage", "deliver-dunnage" };

        foreach (var itemCode in dunnageItems)
        {
            Assert.IsTrue(
                DeclaredLabels(declarations, itemCode).Contains("Description", StringComparer.Ordinal),
                $"`{itemCode}` no longer declares `Description`. It is declared but provably unfillable, so the live question is whether to remove it — not a silent deletion (FR-095).");

            Assert.IsFalse(
                WaitlistCardCellRules.IsCell(itemCode, "Description"),
                $"`{itemCode}`'s `Description` declaration became a cell again, so a field with no source would be drawn (FR-095).");

            var drawn = DrawnLabels(declarations, itemCode);

            Assert.IsFalse(
                drawn.Contains("Description", StringComparer.Ordinal),
                $"`{itemCode}` draws its `Description` declaration, which has no source anywhere (FR-095).");
        }

        var markup = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Controls",
            "WaitlistLineCardDetailTemplates.xaml"));

        Assert.IsFalse(
            Regex.IsMatch(markup, @"Text=""[^""]*Description[^""]*""", RegexOptions.CultureInvariant),
            "A per-Item template hardcodes a Description cell rather than drawing the labels its row resolved, so the exclusion is no longer the card's own rule (FR-095).");
    }

    // ── The corrections and the new cells ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The four corrected labels are declared under the spelling the card reads, and the spelling they replaced is
    /// gone from those Items only — the coil Items keep `Coil number` because it is their cell, and the two pickups
    /// that keep a `Pickup location` keep it as an undrawn declaration (D33, D37, FR-059).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_DeclaresTheCorrectedLabelsAndDropsTheOldSpellings()
    {
        var declarations = SeedDeclarations();
        var problems = new List<string>();

        foreach (var (itemCode, moves) in s_movedLabels)
        {
            var declared = DeclaredLabels(declarations, itemCode);

            foreach (var (from, to) in moves)
            {
                if (!declared.Contains(to, StringComparer.Ordinal))
                {
                    problems.Add($"`{itemCode}` does not declare `{to}`, so its card has no cell to read (FR-059).");
                }

                if (declared.Contains(from, StringComparer.Ordinal))
                {
                    problems.Add($"`{itemCode}` still declares `{from}`, which `{to}` replaced (D33, FR-059).");
                }
            }
        }

        Assert.AreEqual(
            0,
            problems.Count,
            "The label move is incomplete:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, problems));

        // The spellings that stay, so the move cannot widen into the Items they still belong to (D37).
        Assert.IsTrue(
            DeclaredLabels(declarations, "pickup-coil").Contains("Coil number", StringComparer.Ordinal),
            "The coil Items' own `Coil number` cell was moved out from under them (FR-030).");
        Assert.IsTrue(
            DeclaredLabels(declarations, "deliver-coil").Contains("Coil number", StringComparer.Ordinal),
            "The coil Items' own `Coil number` cell was moved out from under them (FR-031).");
        Assert.IsTrue(
            DeclaredLabels(declarations, "pickup-ncm").Contains("Pickup location", StringComparer.Ordinal),
            "`pickup-ncm`'s undrawn `Pickup location` declaration was removed rather than excluded (FR-056, D37).");
        Assert.IsTrue(
            DeclaredLabels(declarations, "pickup-outside-service").Contains("Pickup location", StringComparer.Ordinal),
            "`pickup-outside-service`'s undrawn `Pickup location` declaration was removed rather than excluded (FR-056, D37).");
    }

    /// <summary>
    /// The new answer-backed cells are declared under the very labels the New Request flow captures them with, so
    /// the step that asks the question and the cell that reads it cannot drift apart (FR-077, FR-081, FR-082,
    /// FR-086, FR-087).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_DeclaresTheNewAnswersUnderTheLabelsTheFlowCaptures()
    {
        var declarations = SeedDeclarations();

        var answerCells = new (string Item, string Label, bool Required)[]
        {
            ("pickup-hopper", NewRequestFlowRules.HopperSizeLabel, true),
            ("deliver-hopper", NewRequestFlowRules.HopperSizeLabel, true),
            ("assist-coil-turn", NewRequestFlowRules.CoilWeightLabel, true),
            ("pickup-fg", NewRequestFlowRules.SkidCountLabel, false),
            ("pickup-ncm", NewRequestFlowRules.SkidCountLabel, false),
            ("pickup-wip", NewRequestFlowRules.SkidCountLabel, false),
            ("pickup-outside-service", NewRequestFlowRules.SkidCountLabel, false),
            ("pickup-die", WaitlistCardCellRules.DieShopAnswerLabel, false),
            ("deliver-die", WaitlistCardCellRules.DieShopAnswerLabel, false),
            ("deliver-wrong-coil", NewRequestFlowRules.IncorrectCoilLabel, false),
            ("deliver-wrong-flatstock", NewRequestFlowRules.IncorrectFlatstockLabel, false),
        };

        foreach (var (item, label, required) in answerCells)
        {
            var field = DeclaredField(declarations, item, label);

            Assert.AreEqual(
                RequestItemFieldDefinition.Sources.Answer,
                field.Source,
                $"`{item}` declares `{label}` as a `{field.Source}` field, so the flow never asks the question this cell reads (FR-077, FR-081, FR-082, FR-086, FR-087).");

            Assert.AreEqual(
                required,
                field.IsRequired,
                required
                    ? $"`{item}`'s `{label}` answer is optional, so a request can be raised without the value its cell needs (FR-086)."
                    : $"`{item}`'s `{label}` answer is required, so a question the requester may legitimately leave alone blocks the flow (FR-055, FR-087).");
        }

        // The die-shop cell is drawn through this one label, so the seed declaring it is what makes the third cell
        // resolve at all (FR-081).
        foreach (var item in new[] { "pickup-die", "deliver-die" })
        {
            Assert.IsTrue(
                WaitlistCardCellRules.IsDieShopField(item, WaitlistCardCellRules.DieShopAnswerLabel),
                $"`{item}`'s `{WaitlistCardCellRules.DieShopAnswerLabel}` declaration is not the field the card draws its die-shop cell from (FR-055, FR-081).");
        }

        // The incorrect part is typed by the operator, so its field moved off the job; the correct part is still the
        // job's own and must not have moved with it (FR-082, FR-088).
        Assert.AreEqual(
            RequestItemFieldDefinition.Sources.Job,
            DeclaredField(declarations, "deliver-wrong-coil", "Correct coil").Source,
            "`Correct coil` no longer reads the requesting job, so the cell has nothing to show (FR-088).");
        Assert.AreEqual(
            RequestItemFieldDefinition.Sources.Job,
            DeclaredField(declarations, "deliver-wrong-flatstock", "Correct flatstock").Source,
            "`Correct flatstock` no longer reads the requesting job, so the cell has nothing to show (FR-088).");
    }

    /// <summary>
    /// The derived cells are declared as job fields the resolver answers, and the two read cells are declared as
    /// cells so the day their read mechanism lands the cell appears with no card change (FR-091, FR-092, FR-090,
    /// FR-034).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_DeclaresTheDerivedCellsAsFieldsTheResolverAnswers()
    {
        var declarations = SeedDeclarations();
        var job = Job().WithDisposition(RequestDisposition.FinishedGoods);

        foreach (var item in new[] { "pickup-component", "deliver-component" })
        {
            AssertDerivedCell(declarations, item, "Item Type", job);
        }

        foreach (var item in new[] { "pickup-fg", "pickup-ncm", "pickup-wip", "pickup-outside-service" })
        {
            AssertDerivedCell(declarations, item, "Part Type", job);
        }

        foreach (var item in new[] { "pickup-component", "deliver-component" })
        {
            Assert.AreEqual(
                RequestItemFieldDefinition.Sources.Job,
                DeclaredField(declarations, item, "Total Qty In House").Source,
                $"`{item}`'s `Total Qty In House` cell is not a job-sourced field, so the read's value could never land in it (FR-090).");

            Assert.IsTrue(
                WaitlistCardCellRules.IsCell(item, "Total Qty In House"),
                $"`{item}`'s `Total Qty In House` declaration is excluded from the card, so the read's value would never be drawn (FR-090).");
        }
    }

    // ── The seed's own shape ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The seed still carries one row for each of the twenty-four Items, its fields stay in declared order without
    /// a gap, and no bare <c>order</c> column appears — the JSON payload's own <c>order</c> key is the only one
    /// (FR-029, Database/Database-Ruleset.md).
    /// </summary>
    [TestMethod]
    public void Audit_RequestItemConfigSeedAlignmentTests_Seed_KeepsOneRowPerItemAndAContiguousDeclaredOrder()
    {
        var declarations = SeedDeclarations();

        foreach (var item in RequestItemCatalog.Items)
        {
            var orders = declarations[item.Id].DetailFields.Select(field => field.Order).ToArray();

            CollectionAssert.AreEqual(
                Enumerable.Range(1, orders.Length).ToArray(),
                orders,
                $"`{item.Id}` declares its fields at orders {string.Join(", ", orders)} rather than 1..{orders.Length}, so its cells would not fill the grid in declared order (FR-026).");
        }

        var seed = SeedText();

        Assert.IsFalse(
            Regex.IsMatch(seed, @"^\s*order\s+(INT|BIGINT)", RegexOptions.CultureInvariant | RegexOptions.Multiline),
            "The seed names a bare `order` column, which the database ruleset bans (Database/Database-Ruleset.md).");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────────────────────────────

    private static void AssertDerivedCell(
        IReadOnlyDictionary<string, RequestItemConfiguration> declarations,
        string itemCode,
        string label,
        RequestJobPartAvailability job)
    {
        var field = DeclaredField(declarations, itemCode, label);

        Assert.AreEqual(
            RequestItemFieldDefinition.Sources.Job,
            field.Source,
            $"`{itemCode}`'s `{label}` cell is not a job-sourced field, so the derivation could never reach it (FR-091, FR-092).");

        var resolved = RequestJobFieldValues.Resolve(label, job);

        Assert.AreEqual(
            RequestDispositionLabels.Of(job.Disposition),
            resolved,
            $"`{itemCode}`'s `{label}` label resolves to nothing from a job whose type has been derived, so the cell is hidden on every card (FR-091, FR-092).");
    }

    private static void AssertSettledTableCoversEveryItem()
    {
        var catalogued = RequestItemCatalog.Items.Select(item => item.Id).ToArray();
        var settled = s_cellsTheCardRequires.Keys.ToArray();

        CollectionAssert.AreEquivalent(
            catalogued,
            settled,
            $"This suite settles {settled.Length} Items while the catalogue carries {catalogued.Length}.");

        Assert.AreEqual(
            RequestItemCatalog.TotalCount,
            settled.Length,
            "The settlement covers a different number of Items than the catalogue, so a clean result would prove nothing (FR-103).");
    }

    /// <summary>The labels one Item's declaration carries, in declared order, read from the shipped seed.</summary>
    private static IReadOnlyList<string> DeclaredLabels(
        IReadOnlyDictionary<string, RequestItemConfiguration> declarations,
        string itemCode) =>
        declarations[itemCode].DetailFields.Select(field => field.Label ?? string.Empty).ToArray();

    /// <summary>The one declared field of an Item carrying a label, or a failure naming what is missing.</summary>
    private static RequestItemFieldDefinition DeclaredField(
        IReadOnlyDictionary<string, RequestItemConfiguration> declarations,
        string itemCode,
        string label)
    {
        var field = declarations[itemCode]
            .DetailFields
            .FirstOrDefault(candidate => string.Equals(candidate.Label, label, StringComparison.Ordinal));

        Assert.IsNotNull(
            field,
            $"`{itemCode}` declares no field labelled `{label}`, so the cell the card reads it under has nothing behind it.");

        return field;
    }

    /// <summary>
    /// The labels the card draws for one Item, with the shipped seed's declarations handed to the card the way a
    /// load hands them over, so what is checked is the real resolution rather than a copy of it (FR-025, FR-026).
    /// </summary>
    private static IReadOnlyList<string> DrawnLabels(
        IReadOnlyDictionary<string, RequestItemConfiguration> declarations,
        string itemCode)
    {
        var order = WaitlistViewViewModel.CreateSessionOrder(
            Shape(itemCode),
            Job(),
            declarations[itemCode]);

        return WaitlistLineCardCell
            .CellsToDraw(order.Cells)
            .Select(cell => cell.Label ?? string.Empty)
            .ToArray();
    }

    private static string CorrectedLabel(string itemCode, string label) =>
        s_movedLabels.TryGetValue(itemCode, out var moves)
            ? moves
                .Where(move => string.Equals(move.From, label, StringComparison.OrdinalIgnoreCase))
                .Select(move => move.To)
                .FirstOrDefault() ?? label
            : label;

    private static string Describe(IReadOnlyList<string> labels) =>
        labels.Count == 0 ? "nothing at all" : string.Join(", ", labels.Select(label => $"`{label}`"));

    /// <summary>Builds a request of the given Item's shape carrying distinctive values a source can supply.</summary>
    private static WaitlistRequest Shape(string itemCode) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = RequestItemCatalog.FindById(itemCode)!.Category.ToString(),
        Item = itemCode,
        InputValue = "FGT0002000",
        Answers = Answers(),
        ActiveSetupJobId = "JOB-9001",
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };

    /// <summary>
    /// Every answer the flow can capture, so an answer-backed cell has a value to draw. The die-shop flag is
    /// deliberately absent: its ticked state is what the die Items' third, label-less cell is drawn from, and this
    /// suite compares labels (FR-055).
    /// </summary>
    private static Dictionary<string, string> Answers() => new(StringComparer.OrdinalIgnoreCase)
    {
        [NewRequestFlowRules.HopperSizeLabel] = "Large",
        [NewRequestFlowRules.CoilWeightLabel] = "5120 lb",
        [NewRequestFlowRules.SkidCountLabel] = "1",
        [NewRequestFlowRules.IncorrectCoilLabel] = "MMC0001000",
        [NewRequestFlowRules.IncorrectFlatstockLabel] = "MMF0000500",
    };

    /// <summary>The requesting job's material, so a declared job label has something to resolve to.</summary>
    private static RequestJobPartAvailability Job() => RequestJobPartAvailability.All.WithDisposition(RequestDisposition.FinishedGoods) with
    {
        CoilPartNumber = "MMC0001000",
        CoilDescription = "38 in x 4200 lb",
        CoilAverageWeight = "4200 lb",
        FlatstockPartNumber = "MMF0000500",
        JobPartNumber = "MMC0001000",
        ScrapType = "3003 Aluminum",
        LuggerLocation = "Door 6 Lugger",
        DieNumber = "FGT0002000",
        DieLocation = "DIE SHOP",
        DunnageParts = [new RequestDunnagePart { PartNumber = "DUN0000100", HomeLocation = "A-12" }],
        ComponentFacts = new Dictionary<string, RequestComponentFacts>(StringComparer.OrdinalIgnoreCase)
        {
            ["FGT0002000"] = new RequestComponentFacts("Rear bracket", string.Empty),
        },
    };

    /// <summary>
    /// The shipped declaration seed, parsed once into the configuration the card is handed at run time: every
    /// catalogued Item's declared fields, in declared order, with the source and the required flag each declares.
    /// </summary>
    private static readonly Lazy<Dictionary<string, RequestItemConfiguration>> s_seedDeclarations =
        new(ReadSeedDeclarations);

    private static Dictionary<string, RequestItemConfiguration> SeedDeclarations() => s_seedDeclarations.Value;

    private static string SeedText() => File.ReadAllText(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Database",
        "Seeds",
        "seed_waitlist_request_item_configs",
        "create.sql"));

    private static Dictionary<string, RequestItemConfiguration> ReadSeedDeclarations()
    {
        var seed = SeedText();
        var declarations = new Dictionary<string, RequestItemConfiguration>(StringComparer.OrdinalIgnoreCase);
        var fields = 0;

        foreach (var item in RequestItemCatalog.Items)
        {
            var row = SeedRow(seed, item.Id);
            var parsed = Regex
                .Matches(
                    row,
                    @"JSON_OBJECT\('label','(?<label>[^']*)',(?<body>[^)]*)\)",
                    RegexOptions.CultureInvariant)
                .Select(match => new RequestItemFieldDefinition
                {
                    Label = match.Groups["label"].Value,
                    Source = Regex.Match(match.Groups["body"].Value, @"'source','(?<source>[^']*)'", RegexOptions.CultureInvariant).Groups["source"].Value,
                    Order = int.Parse(
                        Regex.Match(match.Groups["body"].Value, @"'order',(?<order>\d+)", RegexOptions.CultureInvariant).Groups["order"].Value,
                        CultureInfo.InvariantCulture),
                    IsRequired = string.Equals(
                        Regex.Match(match.Groups["body"].Value, @"'is_required',(?<required>TRUE|FALSE)", RegexOptions.CultureInvariant).Groups["required"].Value,
                        "TRUE",
                        StringComparison.Ordinal),
                })
                .ToArray();

            Assert.IsTrue(parsed.Length > 0, $"The shipped seed no longer declares any field for `{item.Id}`.");

            fields += parsed.Length;
            declarations[item.Id] = new RequestItemConfiguration { Item = item.Id, DetailFields = parsed };
        }

        Assert.IsTrue(
            fields >= 60,
            $"The seed parse reached only {fields} declared fields, so a clean result would prove nothing.");

        return declarations;
    }

    /// <summary>
    /// The shipped INSERT row for one Item, from its Item code to the row's own terminator. Line endings are
    /// normalised first, because the row boundary is the blank line between two <c>VALUES</c> entries.
    /// </summary>
    private static string SeedRow(string seedText, string itemCode)
    {
        var normalized = seedText.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = normalized.IndexOf("'" + itemCode + "'", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"The shipped configuration seed no longer carries a row for {itemCode}.");

        var end = normalized.IndexOf("),\n\n", start, StringComparison.Ordinal);

        return end < 0 ? normalized[start..] : normalized[start..end];
    }
}
