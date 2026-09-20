using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// US2 checks (<c>contracts/verification-gates.md</c> G4 #3, #4 and #4b). The confirm step may carry no
/// figure it cannot substantiate, and the coil weight may never be resolved from a fixed key or stand in
/// with a fixed value.
/// </summary>
[TestClass]
public sealed class NewRequestSummaryHonestyTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// The confirm step must have no member that could carry a queue count or a wait estimate until a real
    /// figure exists — the shape of the removed card, not just its two strings.
    /// </summary>
    [TestMethod]
    public void ConfirmStepViewModel_ExposesNoQueueOrWaitEstimateMember()
    {
        var memberNames = typeof(NewRequestSummaryViewModel)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(member => member.Name)
            .ToList();

        string[] forbiddenFragments = ["QueueCount", "QueueDepth", "EstimatedWait", "WaitEstimate", "WaitTime", "ActiveRequestCount"];

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(
                memberNames.Any(name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)),
                $"The confirm step still exposes a '{fragment}' member, so an unsubstantiated figure can be bound again.");
        }
    }

    /// <summary>
    /// And the page itself must carry no such figure as static markup.
    /// </summary>
    [TestMethod]
    public void ConfirmStepPage_CarriesNoQueueOrWaitFigure()
    {
        var markup = File.ReadAllText(ConfirmPagePath());

        (string Description, string Pattern)[] forbidden =
        [
            ("an active-request count", @"active\s+request"),
            ("an estimated wait time", @"estimated\s+wait"),
            ("a wait figure in minutes", @"\b\d+\s*minute"),
            ("a queue heading", @"queue\s*&amp;\s*wait|queue\s+and\s+wait"),
        ];

        foreach (var (description, pattern) in forbidden)
        {
            Assert.IsFalse(
                Regex.IsMatch(markup, pattern, RegexOptions.IgnoreCase),
                $"The confirm step still renders {description}.");
        }
    }

    /// <summary>
    /// The average coil weight may not be shown as a labelled shell: with no resolved value the whole row
    /// goes, the label with it.
    /// </summary>
    [TestMethod]
    public void ConfirmStepPage_GatesTheAverageCoilWeightRowOnHavingAValue()
    {
        var document = XDocument.Load(ConfirmPagePath());
        var label = document
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(element => element.Attribute("Text")?.Value.Contains("Average coil weight", StringComparison.Ordinal) == true);

        Assert.IsNotNull(label, "The average-coil-weight row moved. Update this check rather than deleting it.");
        Assert.IsTrue(
            label.Attribute("Visibility")?.Value.Contains("HasCoilAverageWeight", StringComparison.Ordinal) == true,
            "The average-coil-weight label must be hidden when no value resolved, so an empty labelled row cannot render.");
    }

    /// <summary>
    /// With no coil on the job, every coil field is empty — in every state, never a stand-in value.
    /// </summary>
    [TestMethod]
    public void ConfirmStepViewModel_WithNoResolvedCoil_LeavesEveryCoilFieldEmpty()
    {
        var viewModel = BuildViewModel(new NoCoilAvailabilityService());

        viewModel.OnNavigatedTo(CoilState());

        Assert.IsFalse(viewModel.IsCoilVisible, "No coil resolved, so the coil block must not be shown.");
        Assert.AreEqual(string.Empty, viewModel.CoilNumber);
        Assert.AreEqual(string.Empty, viewModel.CoilQuantityOnHand);
        Assert.AreEqual(string.Empty, viewModel.CoilDescription);
        Assert.AreEqual(string.Empty, viewModel.CoilAverageWeight);
        Assert.IsFalse(
            viewModel.HasCoilAverageWeight,
            "The average weight must report itself absent rather than render a value nobody resolved.");
        Assert.AreEqual(
            0,
            FabricatedValueGuard.FindFabricatedLooking([viewModel.CoilNumber, viewModel.CoilQuantityOnHand, viewModel.CoilDescription, viewModel.CoilAverageWeight]).Count,
            "No coil field may carry a fabricated-looking value.");
    }

    /// <summary>
    /// With a resolved coil, every value is the one the coil read returned.
    /// </summary>
    [TestMethod]
    public void ConfirmStepViewModel_WithAResolvedCoil_ShowsTheResolvedValues()
    {
        var coil = new WaitlistCoilInfo
        {
            HasCoil = true,
            CoilNumber = "MMC778812",
            QuantityOnHand = "4310",
            Description = "COIL 0.050 X 48.0 GALV",
            AverageWeight = "4180 lb",
        };

        var viewModel = BuildViewModel(new FixedCoilAvailabilityService(coil));

        viewModel.OnNavigatedTo(CoilState());

        Assert.IsTrue(viewModel.IsCoilVisible);
        Assert.AreEqual("MMC778812", viewModel.CoilNumber);
        Assert.AreEqual("4310", viewModel.CoilQuantityOnHand);
        Assert.AreEqual("COIL 0.050 X 48.0 GALV", viewModel.CoilDescription);
        Assert.AreEqual("4180 lb", viewModel.CoilAverageWeight);
        Assert.IsTrue(viewModel.HasCoilAverageWeight);
    }

    /// <summary>
    /// The card belongs to the request that asks for the coil, so a request asking for something else must not grow
    /// one — not even on a job that happens to carry a coil.
    /// </summary>
    /// <remarks>
    /// Found by walking the running app on 2026-09-20: a deliver-die request showed its summary and then a coil card
    /// appeared a couple of seconds later, when the asynchronous coil read came back. The card starts hidden, so the
    /// page flickered from right to wrong, and nothing about a die request concerns the job's coil.
    /// </remarks>
    [TestMethod]
    public void ConfirmStep_DieRequestOnAJobCarryingACoil_ShowsNoCoilCard()
    {
        var viewModel = BuildViewModel(new FixedCoilAvailabilityService(ResolvedCoil()));

        viewModel.OnNavigatedTo(DieState());

        Assert.IsFalse(viewModel.IsCoilVisible, "a die request has nothing to do with the job's coil.");
        Assert.IsFalse(viewModel.HasCoilAverageWeight);
        Assert.AreEqual(string.Empty, viewModel.CoilNumber);
    }

    [TestMethod]
    public void ConfirmStep_DieRequestOnAJobCarryingACoil_NeverReadsTheCoil()
    {
        // The card cannot appear later if the read never happens, which is what stops the page flickering from right
        // to wrong once the asynchronous read lands.
        var coil = new RecordingCoilAvailabilityService(ResolvedCoil());
        var viewModel = BuildViewModel(coil);

        viewModel.OnNavigatedTo(DieState());

        Assert.AreEqual(0, coil.ReadCount, "the coil is not read for a request that does not involve it.");
    }

    [TestMethod]
    public void ConfirmStep_CoilRequestOnAJobCarryingACoil_StillShowsTheCoilCard()
    {
        // The guard on the fix: the card is still drawn where it belongs.
        var viewModel = BuildViewModel(new FixedCoilAvailabilityService(ResolvedCoil()));

        viewModel.OnNavigatedTo(CoilState());

        Assert.IsTrue(viewModel.IsCoilVisible);
        Assert.AreEqual("MMC778812", viewModel.CoilNumber);
    }

    [TestMethod]
    public void ConfirmStep_CoilRequestOnAJobWithNoCoil_ShowsNoCoilCard()
    {
        var viewModel = BuildViewModel(new NoCoilAvailabilityService());

        viewModel.OnNavigatedTo(CoilState());

        Assert.IsFalse(viewModel.IsCoilVisible, "a job with no coil has no coil to show.");
    }

    private static WaitlistCoilInfo ResolvedCoil() => new()
    {
        HasCoil = true,
        CoilNumber = "MMC778812",
        QuantityOnHand = "4310",
        Description = "COIL 0.050 X 48.0 GALV",
        AverageWeight = "4180 lb",
    };

    /// <summary>
    /// A die request against a job that <b>does</b> carry a coil — the combination that used to grow a coil card.
    /// </summary>
    private static NewRequestFlowState DieState() => new()
    {
        WorkCenter = "Expo Line 7",
        Category = RequestCategory.Deliver,
        Item = RequestItemCatalog.FindById("deliver-die"),
        Availability = RequestJobPartAvailability.All,
    };

    private static NewRequestFlowState CoilState() => new()
    {
        WorkCenter = "Expo Line 7",
        Category = RequestCategory.Deliver,
        Item = RequestItemCatalog.FindById("deliver-coil"),
        Availability = RequestJobPartAvailability.All,
        InputValue = "Skid 4471 is on the wrong dock",
    };

    private static NewRequestSummaryViewModel BuildViewModel(ICoilAvailabilityService coilAvailabilityService)
        => new(new WaitlistTestNavigationService(), new NoOpRequestService(), coilAvailabilityService);

    private static string ConfirmPagePath()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Module_Waitlist",
            "Views",
            "NewRequestSummaryPage.xaml");

        Assert.IsTrue(File.Exists(path), $"The confirm-step markup was not found at '{path}'.");

        return path;
    }

    private sealed class NoCoilAvailabilityService : ICoilAvailabilityService
    {
        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
            => Task.FromResult(new WaitlistCoilInfo { HasCoil = false });
    }

    private sealed class FixedCoilAvailabilityService(WaitlistCoilInfo coil) : ICoilAvailabilityService
    {
        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
            => Task.FromResult(coil);
    }

    /// <summary>A coil source that counts how many times it was asked, so "the card never appears" can be proven.</summary>
    private sealed class RecordingCoilAvailabilityService(WaitlistCoilInfo coil) : ICoilAvailabilityService
    {
        public int ReadCount { get; private set; }

        public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(coil);
        }
    }

    private sealed class NoOpRequestService : IWaitlistRequestService
    {
        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [];

        public WaitlistRequest? GetRequest(Guid requestId) => null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, bool allowDuplicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);
    }
}
