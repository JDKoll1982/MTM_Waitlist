using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Navigation;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Services;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// FR-082 to FR-085: on the two wrong-part Items the incorrect part is typed through a fuzzy search over a
/// <b>live</b> plant-system read — upper-cased, and zero-padded to ten characters when the term starts with a
/// known prefix — with an exact match resolving at once and anything else offering every part whose number
/// contains the term. When the plant system cannot be reached the operator is told the search is unavailable and
/// the value they typed is used, which is the designed behaviour rather than an error path (FR-085).
/// </summary>
[TestClass]
public sealed class NewRequestPartSearchViewModelTests
{
    /// <summary>The wrong-coil Item's declared label for the incorrect part (data-model §9, §10).</summary>
    private const string IncorrectCoilLabel = "Coil number";

    [DataTestMethod]
    [DataRow("mmc1000", "MMC0001000")]
    [DataRow("MMF500", "MMF0000500")]
    [DataRow("fgt2000", "FGT0002000")]
    [DataRow("MMC0001000", "MMC0001000")]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_ZeroPadsATermThatStartsWithAKnownPrefix(
        string typed,
        string expected)
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok(Rows(expected)));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = typed;
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            expected,
            executor.LastParameters!["Term"],
            "The term the live read is asked about is upper-cased and zero-padded to ten characters (FR-082).");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_AStartsWithNoKnownPrefixIsUpperCasedAndLeftAsTyped()
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok(Rows("ABC-12")));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = "abc-12";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "ABC-12",
            executor.LastParameters!["Term"],
            "A term with no known prefix is upper-cased and not padded, because the prefix is what says how wide the number is (FR-082).");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_AsksTheLivePlantSystemReadForPartMatches()
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Ok(Rows("MMC0001000")));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = "MMC1000";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(
            PartMatchSearchService.ScriptRelativePath,
            executor.LastScriptPath,
            "The search is the new live read, loaded from the checked-in queue script exactly like the other reads (FR-084).");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_AnExactMatchResolvesAtOnceWithoutOfferingAList()
    {
        var executor = new FakeVisualQueryExecutor(
            VisualQueryOutcome.Ok(Rows("MMC0001000", "MMC0001001")));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = "MMC1000";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreEqual(0, viewModel.Matches.Count, "An exact match resolves at once rather than offering a list (FR-083).");
        Assert.AreEqual("MMC0001000", viewModel.SelectedPart, "The exact match is the resolved part.");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_ATermMatchingNothingExactlyOffersEveryPartThatContainsIt()
    {
        var executor = new FakeVisualQueryExecutor(
            VisualQueryOutcome.Ok(Rows("MMC0001000", "MMC0001001", "MMC0001002")));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = "MMC100";
        await viewModel.SearchCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(
            new[] { "MMC0001000", "MMC0001001", "MMC0001002" },
            viewModel.Matches.Select(match => match.Title).ToArray(),
            "A term matching nothing exactly offers every part whose number contains it (FR-083).");
        Assert.IsNull(viewModel.SelectedPart, "Nothing is chosen out of the list on the operator's behalf.");
        Assert.IsFalse(viewModel.IsStatusVisible, "A list of matches is an answer, not something to report.");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_WhenThePlantSystemCannotBeReachedTellsTheOperatorAndKeepsTheTypedValue()
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Unreachable("no route to host"));
        var navigation = new RecordingNavigationService();
        var state = WrongCoilState();
        var viewModel = CreateViewModel(executor, navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.TypedPart = "mmc1000";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsStatusVisible, "The operator is told the search is unavailable (FR-085).");
        Assert.IsFalse(
            viewModel.StatusMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");
        Assert.AreEqual(0, viewModel.Matches.Count, "No list is invented when the plant system cannot be reached.");

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "MMC0001000",
            state.AnswerFor(IncorrectCoilLabel),
            "The value the operator typed is used as entered through the search's own input rule (FR-085, FR-082).");
        Assert.AreEqual(
            typeof(NewRequestDetailsViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "An unavailable search degrades rather than blocking the request; the row's remaining answer is asked next (FR-085).");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_AFaultedReadIsReportedAndStillUsesTheTypedValue()
    {
        var executor = new FakeVisualQueryExecutor(VisualQueryOutcome.Failed("the script was not found"));
        var state = WrongCoilState();
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(state);

        viewModel.TypedPart = "abc-12";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.IsTrue(viewModel.IsStatusVisible, "A fault is reported rather than masked as an empty result.");
        Assert.IsFalse(
            viewModel.StatusMessage.StartsWith("NewRequest_", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key (FR-022).");

        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "ABC-12",
            state.AnswerFor(IncorrectCoilLabel),
            "A faulted search still lets the operator proceed with the number they typed.");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_SearchAsync_RunsTheLiveReadOffTheCallingThread()
    {
        var callerThread = Environment.CurrentManagedThreadId;
        var executor = new ThreadRecordingExecutor(VisualQueryOutcome.Ok(Rows("MMC0001000")));
        var viewModel = CreateViewModel(executor);
        viewModel.OnNavigatedTo(WrongCoilState());

        viewModel.TypedPart = "MMC1000";
        await viewModel.SearchCommand.ExecuteAsync(null);

        Assert.AreNotEqual(
            callerThread,
            executor.ReadThreadId,
            "The live read must not run on the thread that asked for it (T074).");
    }

    [TestMethod]
    public void ViewModel_NewRequestPartSearchViewModelTests_Continue_WithNothingTypedRefusesAndStaysOnTheStep()
    {
        var navigation = new RecordingNavigationService();
        var state = WrongCoilState();
        var viewModel = CreateViewModel(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.ContinueCommand.Execute(null);

        Assert.IsFalse(state.HasAnswer(IncorrectCoilLabel), "No answer is recorded for nothing typed.");
        Assert.AreEqual(0, navigation.Navigations.Count, "The step must not advance without a part (FR-026).");
        Assert.IsTrue(viewModel.IsStatusVisible, "The step says why it will not move on.");
    }

    [TestMethod]
    public async Task ViewModel_NewRequestPartSearchViewModelTests_Continue_RecordsTheChosenPartUnderTheDeclaredLabelAndMovesOn()
    {
        var executor = new FakeVisualQueryExecutor(
            VisualQueryOutcome.Ok(Rows("MMC0001000", "MMC0001001")));
        var navigation = new RecordingNavigationService();
        var state = WrongCoilState();
        var viewModel = CreateViewModel(executor, navigation);
        viewModel.OnNavigatedTo(state);

        viewModel.TypedPart = "MMC100";
        await viewModel.SearchCommand.ExecuteAsync(null);
        viewModel.SelectMatchCommand.Execute(viewModel.Matches[1]);
        viewModel.ContinueCommand.Execute(null);

        Assert.AreEqual(
            "MMC0001001",
            state.AnswerFor(IncorrectCoilLabel),
            "The chosen part is captured under the label the Item declares, which is what the card's cell reads (FR-083).");
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(state.InputValue),
            "The incorrect part is an answer beyond the request's single answer, so it never takes it over (data-model §10).");
        Assert.AreEqual(
            typeof(NewRequestDetailsViewModel).FullName,
            navigation.Navigations[0].PageKey,
            "With the part captured the flow moves on to the answer the row still requires.");
    }

    [TestMethod]
    public void ViewModel_NewRequestPartSearchViewModelTests_OnNavigatedTo_WithoutAStateLeavesTheStep()
    {
        var navigation = new RecordingNavigationService();
        var viewModel = CreateViewModel(new FakeVisualQueryExecutor(VisualQueryOutcome.Ok([])), navigation);

        viewModel.OnNavigatedTo("not a flow state");

        Assert.AreEqual(1, navigation.GoBackCount, "The step cannot run without the request in progress.");
    }

    [TestMethod]
    public void ViewModel_NewRequestPartSearchViewModelTests_TheSearch_IsLiveOnlyWithNoMirrorDeclarationAndNoCachedShape()
    {
        // FR-084: a contains-search over every part is not a mirrorable shape, so this read must never gain a
        // catalog entry — and the service must never be given the fallback that would serve cached rows.
        Assert.IsTrue(
            File.Exists(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), PartMatchSearchService.ScriptRelativePath)),
            "The live read's checked-in script must exist, because a missing script reads to the operator as an outage.");

        var catalogued = VisualReadShapeCatalog.Create()
            .Any(shape => string.Equals(
                shape.SourceScriptRelativePath,
                PartMatchSearchService.ScriptRelativePath,
                StringComparison.OrdinalIgnoreCase));

        Assert.IsFalse(
            catalogued,
            "The fuzzy part search must have no mirror declaration and no cached read shape (FR-084).");

        var dependencies = typeof(PartMatchSearchService)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { typeof(IVisualQueryExecutor) },
            dependencies,
            "The search depends on the live executor alone: giving it the cached fallback would make a stale part list possible (FR-084).");
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows(params string[] partNumbers) =>
        partNumbers
            .Select(number => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["PartNumber"] = number,
            })
            .ToArray();

    private static NewRequestFlowState WrongCoilState() => new()
    {
        WorkCenter = "100-07",
        Category = RequestCategory.Deliver,
        Item = RequestItemCatalog.FindById("deliver-wrong-coil"),
        ItemConfiguration = new RequestItemConfiguration
        {
            Item = "deliver-wrong-coil",
            ControlFlow = RequestItemConfiguration.CollectInputThenConfirm,
            RequiresAnswer = true,
            AnswerValueType = RequestItemValueType.Text,
            DetailFields =
            [
                new RequestItemFieldDefinition
                {
                    Label = "Reason",
                    ValueType = RequestItemValueType.Text,
                    Source = RequestItemFieldDefinition.Sources.Answer,
                    Order = 1,
                    IsRequired = true,
                },
                new RequestItemFieldDefinition
                {
                    Label = IncorrectCoilLabel,
                    ValueType = RequestItemValueType.String,
                    Source = RequestItemFieldDefinition.Sources.Answer,
                    Order = 2,
                },
            ],
        },
    };

    private static NewRequestPartSearchViewModel CreateViewModel(
        IVisualQueryExecutor executor,
        RecordingNavigationService? navigation = null) =>
        new(
            navigation ?? new RecordingNavigationService(),
            new PartMatchSearchService(executor));

    /// <summary>An executor that records the thread its read was asked to run on.</summary>
    private sealed class ThreadRecordingExecutor : IVisualQueryExecutor
    {
        private readonly VisualQueryOutcome _outcome;

        public ThreadRecordingExecutor(VisualQueryOutcome outcome) => _outcome = outcome;

        public int ReadThreadId { get; private set; }

        public Task<VisualQueryOutcome> ExecuteAsync(
            string sourceScriptRelativePath,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            ReadThreadId = Environment.CurrentManagedThreadId;
            return Task.FromResult(_outcome);
        }
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<(string PageKey, object? Parameter)> Navigations { get; } = new();

        public int GoBackCount { get; private set; }

        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => true;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            Navigations.Add((pageKey, parameter));
            return true;
        }

        public bool GoBack()
        {
            GoBackCount++;
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
