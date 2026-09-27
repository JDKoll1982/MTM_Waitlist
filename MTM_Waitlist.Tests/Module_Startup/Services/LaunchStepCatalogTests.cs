using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The catalogue's two promises: every step states a maximum and none is unbounded, and the displayed count is
/// derived from the list rather than stored (`contracts/launch-step-contract.md` §1; plan D21; FR-002, FR-003,
/// FR-026, FR-027, SC-002).
/// </summary>
/// <remarks>
/// <para>
/// Every bound is asserted against real data — the sequence the pipeline will walk — rather than against a
/// fixture, because the point of the catalogue being data is that the numbers it declares are the numbers the
/// launch uses.
/// </para>
/// <para>
/// The validator's own tests use deliberately broken sequences, so a rule that stopped biting is caught here
/// rather than in a launch on a shop-floor machine. The derived-count check reads the catalogue's source, because
/// a stored total and a computed one look identical from the outside until the step list changes.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchStepCatalogTests
{
    private readonly LaunchStepCatalog _catalog = new();

    /// <summary>The catalogue's own source, which is the whole scope of the derived-count check.</summary>
    private static RepositoryScanScope CatalogSourceScope => new()
    {
        IncludeUnder = [Path.Combine("MTM_Waitlist.Startup", "Services", "LaunchStepCatalog.cs")],
    };

    [TestMethod]
    public void CatalogSourceScope_CoversTheCatalogueSource()
    {
        // A scan whose scope resolves to nothing finds nothing, so the derived-count check would pass without
        // reading a file. This proves the scope reaches the catalogue before that check is believed.
        var hits = RepositoryPatternScan.Scan(
            [("the catalogue itself", @"public sealed class LaunchStepCatalog")],
            CatalogSourceScope);

        Assert.AreEqual(1, hits.Count, "the derived-count check's scope does not cover the catalogue source");
    }

    [TestMethod]
    public void Steps_EveryStep_DeclaresAStatedMaximum()
    {
        // Act / Assert: no wait on the launch path may be unbounded (FR-003).
        Assert.IsTrue(_catalog.Steps.Count > 0, "an empty sequence would make every check below vacuous");

        foreach (var step in _catalog.Steps)
        {
            Assert.IsTrue(
                step.MaximumWait > TimeSpan.Zero,
                $"'{step.Id}' declares no maximum wait, so the launch could sit on it without end");
        }
    }

    [TestMethod]
    public void Steps_NoStep_WaitsPastTheCeiling()
    {
        // Act / Assert: 30 seconds is the ceiling for any single wait (FR-003, SC-002).
        foreach (var step in _catalog.Steps)
        {
            Assert.IsTrue(
                step.MaximumWait <= LaunchStepCatalog.Ceiling,
                $"'{step.Id}' is bounded to {step.MaximumWait.TotalSeconds:0.#} seconds, past the ceiling");
        }
    }

    [TestMethod]
    public void Steps_TheTwoBestEffortSteps_AreBoundedMoreTightlyThanTheCeiling()
    {
        // Arrange
        var bestEffort = _catalog.Steps.Where(step => step.IsBestEffort).ToArray();

        // Act / Assert: neither may hold the launch, so neither may sit at the ceiling (FR-026, FR-027).
        Assert.AreEqual(2, bestEffort.Length, "exactly two steps are best effort");
        foreach (var step in bestEffort)
        {
            Assert.IsTrue(
                step.MaximumWait < LaunchStepCatalog.Ceiling,
                $"'{step.Id}' is best effort but is bounded to the ceiling, so it could hold the launch open");
        }
    }

    [TestMethod]
    public void Steps_TheBestEffortSteps_AreThePictureRefreshAndTheExternalSystemPriming()
    {
        // Act
        var bestEffort = _catalog.Steps.Where(step => step.IsBestEffort).ToArray();

        // Assert: the two best-effort steps are the ones the contract names (FR-026, FR-027).
        CollectionAssert.AreEquivalent(
            new[] { "picture-cache", "visual-priming" },
            bestEffort.Select(step => step.Id).ToArray());

        Assert.AreEqual(LaunchStepCategory.Pictures, _catalog.Find("picture-cache")!.Category);
        Assert.AreEqual(LaunchStepCategory.ExternalSystem, _catalog.Find("visual-priming")!.Category);
    }

    [TestMethod]
    public void Steps_EveryStep_CarriesAStableIdANameAndAPlainLanguageDescription()
    {
        // Act / Assert: the name and the description are what the launch window shows before the work runs
        // (FR-002), and the id is what a retry resumes at (FR-020).
        var ids = new List<string>();

        foreach (var step in _catalog.Steps)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.Id), "a step has no id, and an id is what a retry resumes at");
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.Name), $"'{step.Id}' has no name to show while it runs");
            Assert.IsFalse(string.IsNullOrWhiteSpace(step.Description), $"'{step.Id}' has no description");
            Assert.IsFalse(
                step.Description.TrimStart().StartsWith("step", StringComparison.OrdinalIgnoreCase),
                $"'{step.Id}' reads as a step number rather than plain language (FR-002)");
            ids.Add(step.Id);
        }

        CollectionAssert.AllItemsAreUnique(ids, "two steps share an id, so a retry could resume at the wrong one");
    }

    [TestMethod]
    public void Steps_EveryCategoryTheContractDeclares_IsRepresented()
    {
        // Act
        var unrepresented = Enum.GetValues<LaunchStepCategory>()
            .Where(category => _catalog.Steps.All(step => step.Category != category))
            .ToArray();

        // Assert: the categories group the feed, so a category nothing uses would be dead weight in the contract.
        Assert.AreEqual(
            0,
            unrepresented.Length,
            $"no step belongs to: {string.Join(", ", unrepresented)}");
    }

    [TestMethod]
    public void TotalCount_IsTheLengthOfTheStepList()
    {
        // Act / Assert: the displayed total is derived, which is what stops it going stale when the list changes.
        Assert.AreEqual(_catalog.Steps.Count, _catalog.TotalCount);
        Assert.IsTrue(_catalog.TotalCount > 0);
    }

    [TestMethod]
    public void Find_WithAnIdTheCatalogueHolds_ReturnsThatStep()
    {
        var step = _catalog.Find("store-reachability");

        Assert.IsNotNull(step);
        Assert.AreEqual("Contacting the store", step.Name);
    }

    [TestMethod]
    public void CompletedCount_CountsEachStepThatReachedAnEndOnce()
    {
        // Arrange: two steps have ended, one of them through a failure, and a third has only been announced.
        var feed = new LaunchActivityFeed();
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepStarted));
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepCompleted));
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepStarted));
        feed.Append(Line("store-reachability", LaunchFeedEntryKind.StepFailed));
        feed.Append(Line("shell", LaunchFeedEntryKind.StepStarted));

        // Act
        var completed = _catalog.CompletedCount(feed);

        // Assert
        Assert.AreEqual(2, completed);
        Assert.IsTrue(completed <= _catalog.TotalCount, "the progress can never exceed the list it is counted from");
    }

    [TestMethod]
    public void CompletedCount_ForAStepThatFailedThenSucceeded_CountsItOnce()
    {
        // Arrange: a repeated step is one step, however many lines it took.
        var feed = new LaunchActivityFeed();
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepFailed));
        feed.Append(Line("configuration", LaunchFeedEntryKind.StepCompleted));

        // Act
        var completed = _catalog.CompletedCount(feed);

        // Assert
        Assert.AreEqual(1, completed);
    }

    [TestMethod]
    public void CompletedCount_IgnoresLinesForStepsTheCatalogueDoesNotHold()
    {
        // Arrange: a stray id must not push the progress past the total.
        var feed = new LaunchActivityFeed();
        feed.Append(Line("not-a-step", LaunchFeedEntryKind.StepCompleted));

        // Act
        var completed = _catalog.CompletedCount(feed);

        // Assert
        Assert.AreEqual(0, completed);
    }

    [TestMethod]
    public void Validate_WithTheShippedSequence_IsAccepted()
    {
        // Act / Assert: the rule below is exercised by the shipped data, not only by the broken samples.
        Assert.IsTrue(_catalog.Steps.Count > 0);
        LaunchStepCatalog.Validate(_catalog.Steps);
    }

    [TestMethod]
    public void Validate_WithAnUnboundedStep_IsRefused()
    {
        // Arrange
        LaunchStep[] steps = [Step("configuration", TimeSpan.Zero, bestEffort: false)];

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate(steps));
    }

    [TestMethod]
    public void Validate_WithABestEffortStepAtTheCeiling_IsRefused()
    {
        // Arrange: a best-effort step at the ceiling could hold the launch open, which FR-026 forbids.
        LaunchStep[] steps = [Step("picture-cache", LaunchStepCatalog.Ceiling, bestEffort: true)];

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate(steps));
    }

    [TestMethod]
    public void Validate_WithAStepPastTheCeiling_IsRefused()
    {
        // Arrange
        LaunchStep[] steps = [Step("store-reachability", LaunchStepCatalog.Ceiling + TimeSpan.FromSeconds(1), bestEffort: false)];

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate(steps));
    }

    [TestMethod]
    public void Validate_WithARepeatedId_IsRefused()
    {
        // Arrange
        LaunchStep[] steps =
        [
            Step("configuration", TimeSpan.FromSeconds(5), bestEffort: false),
            Step("configuration", TimeSpan.FromSeconds(5), bestEffort: false),
        ];

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate(steps));
    }

    [TestMethod]
    public void Validate_WithAStepThatHasNoName_IsRefused()
    {
        // Arrange
        LaunchStep[] steps =
        [
            new("configuration", "   ", "Reads what this machine needs.", LaunchStepCategory.Configuration, TimeSpan.FromSeconds(5), false),
        ];

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate(steps));
    }

    [TestMethod]
    public void Validate_WithAnEmptySequence_IsRefused()
    {
        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => LaunchStepCatalog.Validate([]));
    }

    [TestMethod]
    public void CatalogSource_DeclaresTheDisplayedCountAsADerivedProperty()
    {
        // Arrange
        var derived = new List<(string Description, string Pattern)>
        {
            ("count computed from the step list", @"int\s+TotalCount\s*=>\s*Steps\.Count"),
        };

        var stored = new List<(string Description, string Pattern)>
        {
            ("count stored as a literal", @"TotalCount\s*=\s*\d"),
        };

        // Act
        var derivedHits = RepositoryPatternScan.Scan(derived, CatalogSourceScope);
        var storedHits = RepositoryPatternScan.Scan(stored, CatalogSourceScope);

        // Assert: a stored total is exactly what leaves a stale "of 5" after a step is added or removed.
        Assert.AreEqual(1, derivedHits.Count, "the displayed total is no longer computed from the step list");
        Assert.AreEqual(0, storedHits.Count, "the displayed total is stored rather than derived");
    }

    /// <summary>One catalogue step, for the validator's own tests.</summary>
    private static LaunchStep Step(string id, TimeSpan maximumWait, bool bestEffort)
        => new(
            id,
            $"{id} name",
            $"{id} description",
            LaunchStepCategory.Configuration,
            maximumWait,
            bestEffort);

    /// <summary>One feed line, with the timestamp taken as it is written.</summary>
    private static LaunchFeedEntry Line(string stepId, LaunchFeedEntryKind kind)
        => new(DateTimeOffset.UtcNow, stepId, kind, $"{stepId} {kind}", null, null);
}
