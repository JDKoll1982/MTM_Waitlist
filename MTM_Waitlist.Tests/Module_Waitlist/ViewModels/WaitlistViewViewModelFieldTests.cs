using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// US1 check 1 (<c>contracts/verification-gates.md</c> G4 #1). A card field value must be traceable to the
/// request on screen; the guard also rejects any value shaped like an invented material attribute, so a
/// substitute re-spelled later still fails.
/// </summary>
/// <remarks>
/// <para>
/// The no-fabrication test is expressed as "every rendered value is a substring of something the request
/// carries". That is the honest form of FR-001/FR-002 for this codebase: <see cref="WaitlistRequest"/> has
/// no material-attribute properties at all, so a card that shows a part, quantity, customer, packlist or
/// destination is showing something no source produced.
/// </para>
/// <para>
/// T029 adds the declared-field half: the card's cells are the Item's declared fields in declared order, a
/// field whose source exists but cannot be read is hidden <b>and</b> recorded in the store's log, and the
/// flyout's condition write is gated on the row and reports the affected rows (FR-058, FR-101, D41).
/// </para>
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelFieldTests
{
    /// <summary>The one answer the flow captured for a request built by <see cref="Shape"/>.</summary>
    private const string DefaultInput = "Skid 4471 is on the wrong dock";

    /// <summary>Every catalogued Item, so the audit covers each shape a request can actually be raised with.</summary>
    private static IReadOnlyList<WaitlistRequest> EveryRequestShape { get; } =
        RequestItemCatalog.Items.Select(item => Shape(item.Id)).ToArray();

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_ForEveryRequestShapeCarriesOnlyValuesTheRequestCarries()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            foreach (var field in order.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)))
            {
                Assert.IsTrue(
                    IsSourced(field.Value, request),
                    $"{Describe(request)}: field '{field.Label}' renders '{field.Value}', which no source on the request produces.");
            }
        }
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_ForEveryRequestShapeRendersNoFabricatedLookingValue()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);
            var offenders = FabricatedValueGuard.FindFabricatedLooking(order.Fields.Select(field => field.Value));

            Assert.AreEqual(
                0,
                offenders.Count,
                $"{Describe(request)}: the card renders fabricated-looking value(s): {string.Join(" | ", offenders)}");
        }
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_ForEveryRequestShapeLeavesAnEmptySlotEmpty()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            foreach (var field in order.Fields.Where(field => string.IsNullOrWhiteSpace(field.Value)))
            {
                Assert.AreEqual(
                    string.Empty,
                    field.Label,
                    $"{Describe(request)}: slot '{field.Label}' has no value, so it must carry no label either — an empty labelled shell is not truthful.");
            }
        }
    }

    /// <summary>
    /// Superseded by US2: the card draws no per-Item field, so a row is no longer padded into a per-type
    /// template's five slots (FR-006, FR-007). The honest check is the reverse of the one this replaces — the
    /// row carries exactly the slots the request produces, and no invented empty-label filler.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_PadsNoRowToATemplatesSlotCount()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            Assert.AreEqual(
                0,
                order.Fields.Count(field => string.IsNullOrWhiteSpace(field.Label) && string.IsNullOrWhiteSpace(field.Value)),
                $"{Describe(request)}: the row carries empty padding slots, which only a per-type template's fixed slot count needed.");
        }
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_SurfacesTheHandlerNeededDataTheRequestCarries()
    {
        // FR-014: a handler must be able to read who asked, where it goes, and how urgent it is off the card
        // itself. Every one of these comes from the request — nothing is derived into a value the request
        // does not carry, which is why the absent ones stay absent rather than being filled in.
        var request = Shape("pickup-ncm");
        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual(request.RequesterEmployeeName, order.RequestedByName, "The handler cannot see who asked.");
        Assert.AreEqual(request.WorkCenter, order.RequestedPressName, "The handler cannot see where the material goes.");
        Assert.IsTrue(order.HasWaitingFor, "The handler cannot see how long the request has been waiting.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(order.RemainingTimeText), "The handler cannot see how urgent the request is.");
        Assert.AreEqual(
            request.RequesterEmployeeNumber,
            order.RequesterEmployeeNumber,
            "The handler cannot see which employee raised the request.");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_AddsNoHandlerSectionToTheCardsFixedLayout()
    {
        // FR-021: handler data is surfaced through the card's existing slots, never by adding a section.
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            Assert.AreEqual(
                5,
                order.Fields.Count,
                $"{Describe(request)}: the card's fixed layout carries five detail slots; this row exposes {order.Fields.Count}.");
        }
    }

    // ── The card's cells: the Item's declared fields, resolved, minus what cannot be read ─────────────

    /// <summary>
    /// The card's cells are the fields the Item declares, one per field that resolved, drawn in declared order
    /// (FR-025, FR-026, <c>data-model.md</c> §7).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_TurnsEachDeclaredFieldIntoACellInDeclaredOrder()
    {
        var declaration = Declaration(
            "pickup-coil",
            ("Average coil weight", RequestItemFieldDefinition.Sources.Job, 4, false),
            ("Coil number", RequestItemFieldDefinition.Sources.Job, 1, false),
            ("Coil description", RequestItemFieldDefinition.Sources.Job, 3, false));

        var order = WaitlistViewViewModel.CreateSessionOrder(Shape("pickup-coil"), CoilJob(), declaration);

        var drawn = WaitlistLineCardCell.CellsToDraw(order.Cells);

        CollectionAssert.AreEqual(
            new[] { "MMC0001000", "38 in x 4200 lb", "4200 lb" },
            drawn.Select(cell => cell.Value).ToArray(),
            "The cells must be the declared fields in declared order, not the order they happen to be resolved in.");

        CollectionAssert.AreEqual(
            new[] { "Coil number", "Coil description", "Average coil weight" },
            drawn.Select(cell => cell.Label).ToArray(),
            "A cell carries the label the declaration carries, never a card-side override.");
    }

    /// <summary>
    /// A field the Item never declares is not a cell, however much of a value the request or the job carries
    /// for it (FR-058, FR-101, D41): the declaration decides which cells exist, not the row.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_ProducesNoCellForAFieldTheItemDoesNotDeclare()
    {
        var declaration = Declaration(
            "pickup-coil",
            ("Coil number", RequestItemFieldDefinition.Sources.Job, 1, false));

        var order = WaitlistViewViewModel.CreateSessionOrder(Shape("pickup-coil"), CoilJob(), declaration);

        Assert.AreEqual(1, order.Cells.Count, "The Item declares one field, so the row carries exactly one cell.");
        Assert.IsFalse(
            order.Cells.Any(cell => string.Equals(cell.Value, "4200 lb", StringComparison.OrdinalIgnoreCase)),
            "The job's average coil weight is a value the request carries, but this Item does not declare a cell for it.");
    }

    /// <summary>
    /// A declared field whose source exists but cannot be read is hidden <b>and</b> recorded in the store's log
    /// so the gap can be chased up later; it is never drawn as a reason and never filled with a substitute
    /// (FR-058, FR-101, D41).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_HidesAFieldWhoseSourceCannotBeReadAndRecordsTheGapInTheStoreLog()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            // A required answer is one the flow itself asked for, so a request that carries none is a source
            // that exists but cannot be read — as opposed to a field nobody declared at all. The die is that
            // answer on the die Items, and it is one of their cells.
            var declaration = Declaration(
                "pickup-die",
                ("Die", RequestItemFieldDefinition.Sources.Answer, 1, true));

            var order = WaitlistViewViewModel.CreateSessionOrder(
                Shape("pickup-die", answers: null, inputValue: null),
                jobAvailability: null,
                declaration);

            Assert.AreEqual(0, order.Cells.Count, "A cell whose source cannot be read is hidden rather than drawn empty.");
            Assert.IsTrue(
                WaitlistLineCardCell.CellsToDraw(order.Cells).Count == 0,
                "Nothing at all is drawn for a field that resolved nothing.");

            Assert.AreEqual(
                1,
                sink.Warnings.Count,
                "The gap must be recorded in the database log once, so it can be chased up later.");
            StringAssert.Contains(
                sink.Warnings[0],
                "Die",
                "The warning must name the declared field that could not be read.");
            StringAssert.Contains(
                sink.Warnings[0],
                "pickup-die",
                "The warning must name the Item whose card is missing a cell.");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    /// <summary>
    /// A source this build cannot read at all is hidden and recorded too, rather than being silently treated as
    /// a field with nothing to say (FR-058, FR-101, D41).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_HidesADeclaredSourceItCannotReadAndRecordsIt()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            var declaration = Declaration(
                "pickup-coil",
                ("CardCellPartType", "read-from-somewhere-else", 1, false));

            var order = WaitlistViewViewModel.CreateSessionOrder(Shape("pickup-coil"), CoilJob(), declaration);

            Assert.AreEqual(0, order.Cells.Count, "A source nothing here reads is hidden rather than guessed at.");
            Assert.AreEqual(1, sink.Warnings.Count, "A declared source that cannot be read is recorded in the store log.");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    /// <summary>
    /// The answer the request captured under the declared label outranks the requesting job for that same label
    /// (FR-082), which is the case the two wrong-part Items need for their typed part.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_PrefersTheCapturedAnswerOverTheJobArmForTheDeclaredLabel()
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Coil number"] = "MMC0009999",
        };

        var declaration = Declaration(
            "deliver-wrong-coil",
            ("Coil number", RequestItemFieldDefinition.Sources.Job, 1, false));

        var order = WaitlistViewViewModel.CreateSessionOrder(
            Shape("deliver-wrong-coil", answers),
            CoilJob(),
            declaration);

        Assert.AreEqual(
            "MMC0009999",
            order.Cells.Single().Value,
            "The part the requester typed is the request's own value, so the job must not answer over the top of it.");
    }

    /// <summary>
    /// The ring's two extra inputs land on the row from the same resolution path that builds its cells, so the
    /// ring's scale is the Item's declared allotment and its measure follows the moment the request closed
    /// (FR-015, FR-017). The fraction itself is re-evaluated on the minute tick that refreshes the row's text.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_FeedsTheRingFromTheItemDeclarationAndTheCloseInstant()
    {
        var declaration = new RequestItemConfiguration
        {
            Item = "pickup-coil",
            AllottedMinutes = 30,
        };

        var live = WaitlistViewViewModel.CreateSessionOrder(Shape("pickup-coil"), CoilJob(), declaration);

        Assert.AreEqual(30, live.AllottedMinutes, "The ring's scale is the Item's declared allotment.");
        Assert.AreEqual(
            TimeRingMeasure.TimeLeft,
            live.RingMeasure,
            "A request nobody has closed yet measures the time it still has left.");

        var closed = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-coil", completedUtc: DateTimeOffset.UtcNow),
            CoilJob(),
            declaration);

        Assert.AreEqual(
            TimeRingMeasure.TimeTaken,
            closed.RingMeasure,
            "The row's close instant is what freezes the ring, so a completed request measures the time it took.");
    }

    /// <summary>
    /// A dunnage request's row carries the picture of the dunnage part its identifier names. The dunnage part is
    /// the one thing such a request is about, and it is not a material part, so the picture is read from the
    /// requesting job here — the only place it exists — rather than resolved by the material-part resolver, which
    /// cannot see the dunnage catalogue. Without it the card drew the placeholder even when the part had a picture.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_CarriesTheChosenDunnagePartsPicture()
    {
        var job = (RequestJobPartAvailability.None with
        {
            HasActiveJob = true,
            HasDunnage = true,
        }).WithDunnageParts(
        [
            new RequestDunnagePart { PartNumber = "DN-STL-4", ImagePath = @"X:\Dunnage\Parts\Steel Rack.png" },
        ]);

        var row = WaitlistViewViewModel.CreateSessionOrder(
            Shape("deliver-dunnage", inputValue: "DN-STL-4"),
            job);

        Assert.AreEqual(
            @"X:\Dunnage\Parts\Steel Rack.png",
            row.ResolvedPartImagePath,
            "The dunnage card draws the picture of the part its own identifier names.");
        Assert.AreEqual(
            @"X:\Dunnage\Parts\Steel Rack.png",
            row.EffectiveImagePath,
            "And that picture is what the card actually draws, rather than the shared placeholder.");
    }

    /// <summary>
    /// A request that is not about a dunnage part keeps the shared placeholder, whatever its answer happens to
    /// spell: the picture rule is asked for every row, so it must not hand a coil card a dunnage part's picture.
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_LeavesANonDunnageRowOnTheSharedPlaceholder()
    {
        var job = (RequestJobPartAvailability.None with
        {
            HasActiveJob = true,
            HasDunnage = true,
        }).WithDunnageParts(
        [
            new RequestDunnagePart { PartNumber = "DN-STL-4", ImagePath = @"X:\Dunnage\Parts\Steel Rack.png" },
        ]);

        var row = WaitlistViewViewModel.CreateSessionOrder(
            Shape("pickup-riser-table", inputValue: "DN-STL-4"),
            job);

        Assert.AreEqual(
            string.Empty,
            row.ResolvedPartImagePath,
            "A request that is not about a dunnage part resolves no dunnage picture.");
        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            row.EffectiveImagePath,
            "It draws the one shared placeholder instead.");
    }

    // ── The flyout's command: one condition, set on the row, by the row's own handler ─────────────────

    /// <summary>
    /// The command the status word's flyout invokes writes the chosen condition through the seam that reports
    /// the affected rows, for the row's own material handler (FR-070, FR-072, FR-076).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelFieldTests_SetStatusConditionCommand_WritesTheChosenConditionForTheRowsOwnHandler()
    {
        var row = new SampleOrder
        {
            RequestId = Guid.NewGuid(),
            AssignedMaterialHandler = "6331",
            CanCompleteOrRelease = true,
        };

        var service = new RecordingRequestService(rowsAffected: 1);
        var viewModel = BuildViewModel(service);

        await viewModel.SetStatusConditionCommand.ExecuteAsync((row, "Product Buried")).ConfigureAwait(false);

        Assert.AreEqual(1, service.SetHandlerStatusCalls.Count, "The row's own handler's choice must reach the store.");
        Assert.AreEqual(row.RequestId, service.SetHandlerStatusCalls[0].RequestId);
        Assert.AreEqual("Product Buried", service.SetHandlerStatusCalls[0].HandlerStatus);
        Assert.AreEqual(
            "6331",
            service.SetHandlerStatusCalls[0].ExpectedMaterialHandler,
            "The write is conditional on the handler the row was read with, so only that handler's write lands.");
        Assert.AreEqual(string.Empty, viewModel.ActionRefusalMessage, "A write that landed is not a refusal.");
    }

    /// <summary>
    /// The word is inert for everyone else (FR-074): a row the signed-in person does not hold writes nothing,
    /// rather than being refused with a message the flyout never opened to show.
    /// </summary>
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelFieldTests_SetStatusConditionCommand_IsInertForARowTheViewerDoesNotHold()
    {
        var row = new SampleOrder
        {
            RequestId = Guid.NewGuid(),
            AssignedMaterialHandler = "9002",
            CanCompleteOrRelease = false,
        };

        var service = new RecordingRequestService(rowsAffected: 1);
        var viewModel = BuildViewModel(service);

        await viewModel.SetStatusConditionCommand.ExecuteAsync((row, "Product Buried")).ConfigureAwait(false);

        Assert.AreEqual(0, service.SetHandlerStatusCalls.Count, "Only the row's own material handler may set its condition.");
        Assert.AreEqual(string.Empty, viewModel.ActionRefusalMessage, "An inert word must not report a refusal.");
    }

    /// <summary>
    /// A write the store did not apply is told rather than shown as success (FR-073): the affected-row count is
    /// the only thing that says whether the condition landed.
    /// </summary>
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelFieldTests_SetStatusConditionCommand_TellsALostRaceRatherThanReportingSuccess()
    {
        var row = new SampleOrder
        {
            RequestId = Guid.NewGuid(),
            AssignedMaterialHandler = "6331",
            CanCompleteOrRelease = true,
        };

        var service = new RecordingRequestService(rowsAffected: 0);
        var viewModel = BuildViewModel(service);

        await viewModel.SetStatusConditionCommand.ExecuteAsync((row, "Changing Forklift")).ConfigureAwait(false);

        Assert.AreEqual(1, service.SetHandlerStatusCalls.Count, "The write was attempted.");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.ActionRefusalMessage),
            "A write that affected no rows must be told rather than passed off as success.");
    }

    /// <summary>
    /// The condition a handler reported survives the list read that comes after it: a stored condition is restored
    /// onto the row the list builds, with the word and the column theme that go with it, so a restart shows the
    /// same card rather than a row nobody reported on (FR-073, SC-016).
    /// </summary>
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelFieldTests_CreateSessionOrder_RestoresAStoredConditionOntoTheRowItBuilds()
    {
        var stored = new WaitlistRequest
        {
            Id = Guid.NewGuid(),
            Building = "Expo Drive",
            WorkCenter = "Press 12",
            Category = "Deliver",
            Item = "deliver-coil",
            Status = "Accepted",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            AssignedMaterialHandler = "6331",
            HandlerStatus = "Changing Propane",
        };

        var row = WaitlistViewViewModel.CreateSessionOrder(stored);

        Assert.AreEqual(
            "Changing Propane",
            row.HandlerStatus,
            "The condition the store holds must be restored onto the row the list next reads, or a restart loses "
                + "what the handler reported (FR-073, SC-016).");
        Assert.AreEqual(
            "Changing Propane",
            row.StatusWord,
            "The restored condition is what row 1 reads, in precedence over the lifecycle word (FR-012).");
        Assert.AreEqual(
            StatusColumnTheme.HandlerCondition,
            row.StatusTheme,
            "A restored condition themes the whole column as the lighter handler red (FR-072).");
    }

    /// <summary>
    /// The load path is what puts the declaration and the row together: a row the list builds carries the cells
    /// its Item declares, resolved from the request the list read (FR-025, FR-026).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelFieldTests_RefreshAsync_BuildsEachRowsCellsFromItsItemsDeclaredFields()
    {
        var request = Shape("other");
        var declaration = Declaration("other", ("Request description", RequestItemFieldDefinition.Sources.Answer, 1, true));
        var viewModel = new WaitlistViewViewModel(
            new WaitlistTestNavigationService(),
            new WaitlistTestBuildingSelectionService(),
            new RecordingRequestService(rowsAffected: 0, request),
            itemConfigurationService: new StubItemConfigurationService(declaration));

        await viewModel.RefreshAsync().ConfigureAwait(false);

        var row = viewModel.Source.Single();
        var cell = row.Cells.Single();

        Assert.AreEqual("Request description", cell.Label, "The cell carries the label the Item's declaration carries.");
        Assert.AreEqual(
            DefaultInput,
            cell.Value,
            "The cell's value is the answer the request itself carries, never a value the screen made up.");
    }

    /// <summary>A navigation service that goes nowhere and a building selection that never changes, so the command can be driven headless.</summary>
    private static WaitlistViewViewModel BuildViewModel(IWaitlistRequestService requestService) => new(
        new WaitlistTestNavigationService(),
        new WaitlistTestBuildingSelectionService(),
        requestService);

    /// <summary>An Item declaration carrying the fields under test, with nothing else claimed about the Item.</summary>
    /// <param name="item">The Item code the declaration belongs to.</param>
    /// <param name="fields">The declared fields, as (label, source, order, is-required).</param>
    private static RequestItemConfiguration Declaration(
        string item,
        params (string Label, string Source, int Order, bool IsRequired)[] fields) => new()
    {
        Item = item,
        DetailFields = fields
            .Select(field => new RequestItemFieldDefinition
            {
                Label = field.Label,
                Source = field.Source,
                Order = field.Order,
                IsRequired = field.IsRequired,
            })
            .ToArray(),
    };

    /// <summary>The requesting job's coil values, so a declared job label has something to resolve to.</summary>
    private static RequestJobPartAvailability CoilJob() => RequestJobPartAvailability.All with
    {
        CoilPartNumber = "MMC0001000",
        CoilDescription = "38 in x 4200 lb",
        CoilAverageWeight = "4200 lb",
    };

    /// <summary>Captures what the facade forwards to the store's log, so a warning can be observed without a store.</summary>
    private sealed class RecordingAppLogSink : IAppLogSink
    {
        public List<string> Warnings { get; } = [];

        public void Info(string module, string message, string? target = null)
        {
        }

        public void Error(string module, string message, Exception? exception = null)
        {
        }

        public void Warn(string module, string message, string? errorType = null) => Warnings.Add(message);
    }

    /// <summary>The write seam as this screen meets it: one answer for the condition write, and nothing else.</summary>
    private sealed class RecordingRequestService : IWaitlistRequestService
    {
        private readonly int _rowsAffected;
        private readonly WaitlistRequest[] _requests;

        public RecordingRequestService(int rowsAffected, params WaitlistRequest[] requests)
        {
            _rowsAffected = rowsAffected;
            _requests = requests;
        }

        public List<(Guid RequestId, string? HandlerStatus, string ExpectedMaterialHandler)> SetHandlerStatusCalls { get; } = [];

        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public Task<int> SetHandlerStatusAsync(
            Guid requestId,
            string? handlerStatus,
            string expectedMaterialHandler,
            CancellationToken cancellationToken = default)
        {
            SetHandlerStatusCalls.Add((requestId, handlerStatus, expectedMaterialHandler));
            return Task.FromResult(_rowsAffected);
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => _requests;

        public WaitlistRequest? GetRequest(Guid requestId) =>
            _requests.FirstOrDefault(request => request.Id == requestId);

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(
            Guid requestId,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(
            WaitlistRequestDraft draft,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<bool> TransitionStatusAsync(
            Guid requestId,
            string status,
            string? cancellationReason = null,
            string? canceledByEmployeeNumber = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(
            Guid requestId,
            string requesterEmployeeNumber,
            string? reason = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<WaitlistRequest?> UpdateNoteAsync(
            Guid requestId,
            string? note,
            string? actorEmployeeNumber = null,
            string? actorEmployeeName = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<WaitlistRequest?> AcceptAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<WaitlistRequest?> CompleteAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");

        public Task<WaitlistRequest?> ReleaseAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException("This test drives one condition write.");
    }

    /// <summary>An Item-declaration reader that answers from one fixed declaration, so the load path can be driven without a store.</summary>
    private sealed class StubItemConfigurationService : IRequestItemConfigurationService
    {
        private readonly RequestItemConfiguration _declaration;

        public StubItemConfigurationService(RequestItemConfiguration declaration) => _declaration = declaration;

        public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RequestItemConfigurationSet.From([_declaration]));

        public Task<RequestItemConfiguration> GetConfigurationAsync(
            string itemCode,
            CancellationToken cancellationToken = default) => Task.FromResult(_declaration);
    }

    /// <summary>Builds a request of the given Item's shape carrying distinctive, non-fabricated values.</summary>
    /// <param name="itemCode">The Item code under test.</param>
    /// <param name="answers">The answers the request captured beyond its one answer, or null for none.</param>
    /// <param name="inputValue">The request's one captured answer, or null for a request that captured none.</param>
    /// <param name="completedUtc">The instant the request was completed, or null while it is still open.</param>
    /// <returns>The request.</returns>
    private static WaitlistRequest Shape(
        string itemCode,
        IReadOnlyDictionary<string, string>? answers = null,
        string? inputValue = DefaultInput,
        DateTimeOffset? completedUtc = null) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = RequestItemCatalog.FindById(itemCode)!.Category.ToString(),
        Item = itemCode,
        InputValue = inputValue,
        Answers = answers,
        CompletedUtc = completedUtc,
        ActiveSetupJobId = "JOB-9001",
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };

    /// <summary>
    /// Whether a rendered value is carried by the request. The request id is compared in both its plain and
    /// its dashed rendering because the card shows one of them.
    /// </summary>
    private static bool IsSourced(string value, WaitlistRequest request)
        => SourcedValues(request).Any(source => source.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> SourcedValues(WaitlistRequest request)
    {
        yield return request.Building;
        yield return request.WorkCenter;
        yield return request.WorkCenterName;
        yield return request.Category;
        yield return request.Item;
        yield return request.InputValue ?? string.Empty;
        yield return request.ActiveSetupJobId;
        yield return request.RequesterEmployeeNumber;
        yield return request.RequesterEmployeeName;
        yield return request.Status;
        yield return request.Id.ToString("N");
        yield return request.Id.ToString("D");
    }

    private static string Describe(WaitlistRequest request) => $"{request.Category} / {request.Item}";
}
