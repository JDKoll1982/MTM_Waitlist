using System.Globalization;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Builds the card row a waitlist request draws: one <see cref="SampleOrder"/> per request, carrying the two
/// lines, the picture and the cells the request and its Item declaration resolve.
/// </summary>
/// <remarks>
/// Both surfaces that show a request build the same row — the open list and the page a row opens — so the mapping
/// lives here rather than on either of them. That is what keeps the two from drifting apart, and it is why the
/// detail page no longer reaches into the list's view model to draw its own row.
/// </remarks>
public static class WaitlistSessionOrderFactory
{
    /// <summary>
    /// Builds the row for one request.
    /// </summary>
    /// <param name="request">The request the row is built from.</param>
    /// <param name="jobAvailability">The requesting job's snapshot, or null before one has arrived.</param>
    /// <param name="declaration">The Item's configuration row, or null when it could not be read.</param>
    /// <param name="arrangement">The lugger arrangement snapshot, or null before one has been read.</param>
    /// <returns>The row the card draws for this request.</returns>
    public static SampleOrder CreateSessionOrder(
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability = null,
        RequestItemConfiguration? declaration = null,
        LuggerArrangementSnapshot? arrangement = null)
    {
        // The request's identity is its Item code. Everything the card shows about *what* was asked for —
        // both lines, the picture, the detail slots — resolves from that one code through the Item catalog,
        // so no reader is handed a second, derived pair it could disagree with (FR-004, FR-005).
        var definition = request.ItemDefinition;
        var context = WaitlistRequestTitles.ResolveContext(request, jobAvailability);
        var line1 = WaitlistRequestTitles.ResolveLine1(definition, context);
        var line2 = WaitlistRequestTitles.ResolveLine2(definition, context);

        // The part this request is about — its material — captured on the row here because this is where the
        // requesting job is in hand. It is the same rule the card's own identifier resolves {part_number} from,
        // so the picture the card draws and the part the card names can never be two different parts (FR-019).
        // A request that names no material carries none, and its card draws the one shared placeholder (FR-020).
        var materialPartNumber = WaitlistRequestTitles.ResolveMaterialPartNumber(request, jobAvailability);

        // A dunnage request carries no material part, but it is just as much about one thing the operator picked:
        // the dunnage part, which is the part its second line names. Its picture is carried here for the same
        // reason the material part's is resolved where the job is in hand — neither surface's picture resolver can
        // see the dunnage catalogue, so a dunnage card would otherwise always draw the placeholder.
        var dunnagePartPicturePath = WaitlistRequestTitles.ResolveDunnagePartPicturePath(context, jobAvailability, request.Answers);

        // A hopper request names no material part either, but it too is about one thing the operator picked: the
        // size they chose, and its word is the key that size's hopper picture is filed under. The size is handed to
        // the picture resolver in place of a part number, in the hopper pictures' own scope rather than a part
        // scope, so the card's left spot draws the very picture the New Request hopper step drew and the card's
        // Size cell opens (FR-077, FR-078, D22). A hopper request with no size captured pictures nothing and keeps
        // the shared placeholder.
        var hopperSize = WaitlistCardCellRules.HopperSizeAnswer(request.Item, request);

        // The scrap Item names no material part, but it names the lugger its material is dumped into — so that
        // lugger is what its card is about, and its picture is what the card's left spot draws. It is carried here
        // for the same reason the hopper size is: the lugger's picture is filed under the lugger's place in the
        // arrangement, which only the availability snapshot in hand can name (owner decision, 2026-10-26).
        // A generic request has no job, so the snapshot's lugger belongs to the job's scrap type and never to the
        // type the requester stated: its card draws the shared placeholder rather than another job's artwork
        // (FR-006, FR-010).
        var luggerImagePath = WaitlistCardCellRules.IsScrapItem(request.Item) && !request.IsGeneric
            ? jobAvailability?.LuggerLocationImagePath ?? string.Empty
            : string.Empty;

        var item = new SampleOrder
        {
            Id = request.Id.GetHashCode(),
            RequestId = request.Id,
            ItemCode = request.Item,
            MaterialPartNumber = materialPartNumber ?? hopperSize ?? string.Empty,
            StockLookupPartNumber = string.Equals(request.Item, "deliver-component", StringComparison.OrdinalIgnoreCase)
                ? context.Component?.Trim() ?? string.Empty
                : string.Empty,
            MaterialPartScope = hopperSize is not null
                ? PartPictureLayout.HopperSizeScope
                : materialPartNumber is null ? string.Empty : PartPictureLayout.VisualPartScope,
            // Empty for every request that is not about a dunnage part, which draws the one shared placeholder.
            // The scrap card's lugger stands in for it for the same reason the dunnage part does: it is the one
            // thing the operator's request is about, and the list would otherwise have no way to picture it.
            ResolvedPartImagePath = dunnagePartPicturePath ?? luggerImagePath,
            RequesterEmployeeNumber = request.RequesterEmployeeNumber,
            AssignedMaterialHandler = request.AssignedMaterialHandler,
            AssignedMaterialHandlerName = request.AssignedMaterialHandlerName,
            Note = request.Note,
            Title = line1,
            Subtitle = line2.Text,
            Line2Problem = line2.IsResolved ? null : line2.Problem,
            Status = request.Status,
            RequestedByName = string.IsNullOrWhiteSpace(request.RequesterEmployeeName) ? "Current user" : request.RequesterEmployeeName,
            RequestedPressName = request.WorkCenter,
            RemainingTimeText = GetRemainingTimeText(request.TargetTimeUtc, request.IsOverdue),
            WaitingForText = GetWaitingForText(request.RequestedUtc),
            RequestedUtc = request.RequestedUtc,
            TargetTimeUtc = request.TargetTimeUtc,
            LastMessageUtc = request.LastMessageUtc,
            // The store's overdue flag is sticky, and a row whose due time has already passed is overdue too, so
            // the setter is never given an answer the row's own first paint would contradict: without this the word
            // on row 1 read `Waiting` while the ring beside it already read `Overdue`, until the first tick ran.
            IsOverdue = SampleOrder.IsOverdueAt(request.IsOverdue, request.TargetTimeUtc),
            IsOverdueAtSource = request.IsOverdue,

            // The condition the row's own material handler reported, read back with the request so it is restored
            // onto the same card on the next list read and survives a restart (FR-073, SC-016). It is copied as
            // the store holds it: the row's own setter is what decides whether the value is one of the four
            // conditions, so a fifth word reads as no condition rather than as a report nobody made.
            HandlerStatus = request.HandlerStatus,

            // A generic request's own facts, read back with the row so its card draws them without a second read
            // (FR-008, FR-022, FR-023). A work-order request carries the flag false and none of the rest, so the
            // board's own card is chosen for it and nothing on it changes.
            IsGeneric = request.IsGeneric,
            Description = request.Description,
            ConfiguredAllottedMinutes = declaration?.AllottedMinutes,

            // The ring's two extra inputs (data-model.md §7, FR-015, FR-017). The allotment is the Item's
            // declared one — the number the urgency deadline service reads from the same configuration row — so
            // the ring's scale and the countdown come from one source, and an Item that declares none keeps the
            // row's own labelled fifteen-minute fallback. The close instant freezes the ring for a Done or
            // Cancelled request; from then on the fraction is the time taken rather than the time left, and the
            // minute tick recomputes the fraction without a second clock.
            AllottedMinutes = declaration?.AllottedMinutes,
            ClosedUtc = request.CompletedUtc ?? request.CanceledUtc,
        };

        // One card row per request: the row carries the two lines, the picture, the four metadata values and
        // the action affordances, and nothing about it selects a layout (FR-006). Its own declared fields are
        // not padded into template slots any more — the card draws no per-Item field, and the ones the request
        // does carry are read by the request page (FR-007).
        AddRequestFields(item, request);

        // The card's own cells are the fields the Item declares, resolved here where the request and the
        // requesting job are both in hand (FR-025, FR-026, §7).
        AddDeclaredCells(item, request, jobAvailability, declaration, arrangement);

        // A generic request's part answer is stored under its own names rather than under a declared field, so the
        // declaration above cannot resolve it. Its three facts are added here, which is what puts a generic card's
        // cells in the same place and the same shape as a work-order card's (owner decision, 2026-11-04).
        AddGenericPartCells(item, request);

        return item;
    }

    /// <summary>
    /// The cells a generic part request adds to its card: the part, the sequence it was requested at, and the units
    /// on hand with the system that answered (FR-018, FR-019).
    /// </summary>
    /// <remarks>
    /// A deadline is deliberately not a cell. The card's ring already carries it for every row, generic or not, and
    /// a fourth cell only on generic cards is exactly the difference this change exists to remove.
    /// </remarks>
    private static void AddGenericPartCells(SampleOrder item, WaitlistRequest request)
    {
        if (!request.IsGeneric
            || MTM_Waitlist.Module_Core.Models.GenericRequestPartAnswer.TryRead(request.Answers) is not { HasPart: true } part)
        {
            return;
        }

        AddCell(item, "Waitlist_Card_Part.Label", part.PartNumber);
        AddCell(item, "Waitlist_Card_Sequence.Label", part.Sequence);

        if (part.QuantityOnHand is { } figure)
        {
            var label = part.AnsweredBy.Length == 0
                ? "Waitlist_Card_OnHand.Label".GetLocalized()
                : string.Format(
                    CultureInfo.CurrentCulture,
                    "Waitlist_Card_OnHandBy.Label".GetLocalized(),
                    SystemName(part.AnsweredBy));

            AddCell(item, label, figure.ToString("0.##", CultureInfo.CurrentCulture));
        }
    }

    /// <summary>The on-hand cell's label, already resolved, added only when it has a value to carry.</summary>
    private static void AddCell(SampleOrder item, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            item.Cells.Add(new WaitlistLineCardCell { Label = label, Value = value });
        }
    }

    /// <summary>The name a person reads for the system that answered a quantity.</summary>
    private static string SystemName(string answeredBy) =>
        string.Equals(answeredBy, "WipFloor", StringComparison.OrdinalIgnoreCase)
            ? "Waitlist_Card_System_WipFloor.Text".GetLocalized()
            : "Waitlist_Card_System_InforVisual.Text".GetLocalized();

    /// <summary>
    /// Fills a row's cells from its Item's declaration: one cell per declared field, in declared order, with
    /// every field that resolved nothing left out entirely (FR-006, FR-026).
    /// </summary>
    private static void AddDeclaredCells(
        SampleOrder item,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability,
        RequestItemConfiguration? declaration,
        LuggerArrangementSnapshot? arrangement)
    {
        foreach (var cell in ResolveDeclaredCells(declaration, request, jobAvailability, arrangement))
        {
            item.Cells.Add(cell);
        }
    }

    /// <summary>
    /// The cells one row draws: one per field the Item's declaration carries <b>that is one of the Item's
    /// cells</b>, resolved by that field's own source and laid out in declared order, with the hide rule's two
    /// halves applied — a cell the Item never declares is never produced, and a cell whose source exists but
    /// cannot be read is not produced either and is recorded in the store's log instead (FR-025, FR-026,
    /// FR-056, FR-058, FR-101, D41).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>WaitlistLineCardCell.CellsToDraw</c> is the one place the "a cell that resolved nothing is not
    /// drawn" rule lives, so this method hands it every declared field's cell and draws what comes back.
    /// </para>
    /// <para>
    /// <c>WaitlistCardCellRules</c> is the one place the other half lives: an Item declares fields its card
    /// does not show, and those are excluded here rather than removed from configuration, which is what keeps
    /// the exclusion a card rule and lets the two disagree without either being quietly edited (FR-056,
    /// <c>data-model.md</c> §9). An excluded field is working as designed, so nothing is logged for it.
    /// </para>
    /// </remarks>
    /// <param name="declaration">The Item's configuration row, or null when it could not be read.</param>
    /// <param name="request">The request the row was built from.</param>
    /// <param name="jobAvailability">The requesting job's snapshot, or null before one has arrived.</param>
    /// <returns>The cells to draw, in declared order; nothing at all when the declaration cannot be read.</returns>
    public static IReadOnlyList<WaitlistLineCardCell> ResolveDeclaredCells(
        RequestItemConfiguration? declaration,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability,
        LuggerArrangementSnapshot? arrangement = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (declaration is null || !declaration.IsAvailable || declaration.DetailFields.Count == 0)
        {
            return Array.Empty<WaitlistLineCardCell>();
        }

        var candidates = new List<WaitlistLineCardCell?>(declaration.DetailFields.Count);
        foreach (var field in declaration.DetailFields)
        {
            if (field is null || !WaitlistCardCellRules.IsCell(request.Item, field.Label))
            {
                continue;
            }

            candidates.Add(ResolveDeclaredCell(field, request, jobAvailability, arrangement));
        }

        return WaitlistCardCellRules.OrderForPanel(request.Item, WaitlistLineCardCell.CellsToDraw(candidates));
    }

    /// <summary>
    /// One declared field as a cell, carrying the declared label as the resource key it is and whatever its
    /// source resolved to. A field whose source exists but cannot be read resolves to nothing — and is recorded
    /// — so it is hidden rather than explained on the card or filled with a substitute (FR-058, FR-101, D41).
    /// </summary>
    private static WaitlistLineCardCell ResolveDeclaredCell(
        RequestItemFieldDefinition? field,
        WaitlistRequest request,
        RequestJobPartAvailability? jobAvailability,
        LuggerArrangementSnapshot? arrangement)
    {
        if (field is null)
        {
            return new WaitlistLineCardCell();
        }

        var label = field.Label ?? string.Empty;
        var source = field.Source?.Trim() ?? string.Empty;

        if (WaitlistCardCellRules.IsDieShopField(request.Item, label))
        {            // The die Items' die-shop cell is the one cell with no label: its phrase IS its value, so what the
            // requester ticked is read here and the phrase — chosen by the Item's direction — is what lands in
            // the cell. An unticked checkbox is no answer, so the third cell stays absent rather than drawing
            // the other half of the choice (FR-055, FR-081). The answer is read as a labelled one, never as
            // the request's single answer, so the die the flow asked for can never be mistaken for this flag.
            var given = CapturedAnswer(label, request, required: false);

            return new WaitlistLineCardCell
            {
                Value = WaitlistCardCellRules.IsDieShopAnswerGiven(given)
                    ? WaitlistCardCellRules.DieShopPhrase(request.Item) ?? string.Empty
                    : string.Empty,
                Order = field.Order,
            };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Answer, StringComparison.OrdinalIgnoreCase))
        {
            var answer = CapturedAnswer(label, request, field.IsRequired);

            if (answer is null && field.IsRequired)
            {
                WarnUnreadableDeclaredField(
                    request,
                    label,
                    "the answer it declares it needs was not captured");
            }

            return new WaitlistLineCardCell
            {
                Label = label,
                Value = answer ?? string.Empty,
                Order = field.Order,

                // The one cell on a hopper card that pictures the answer it shows: the size word is also the key
                // that size's reference picture is filed under, so the card opens the same picture the New
                // Request hopper step drew (FR-077, FR-078, D22). Any other answer-sourced cell opens none.
                ReferencePartNumber = WaitlistCardCellRules.ReferencePicturePart(request.Item, label, answer) ?? string.Empty,
                ReferencePartScope = WaitlistCardCellRules.ReferencePictureScope,
                DrawsPicture = WaitlistCardCellRules.ReferencePicturePart(request.Item, label, answer) is not null,
            };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Job, StringComparison.OrdinalIgnoreCase))
        {
            // A generic work center has no job, so a generic scrap request's two facts come from the request's own
            // answer and from the arrangement read for this load, resolved by the same rule the work-order card's
            // lugger came from (FR-006, FR-007). Every other job-sourced cell still reads the job.
            var isGenericScrap = WaitlistCardCellRules.IsScrapItem(request.Item) && request.IsGeneric;

            // A label the requesting job does not carry is the designed absence, not a fault: a job with no die
            // draws no Die cell and nothing is logged for it (FR-057, RequestJobFieldValues). The label that
            // resolves is the declared one, and an answer captured under it outranks the job's own (FR-082).
            var jobValue = isGenericScrap
                ? ResolveStatedScrapCell(label, request, arrangement)
                : ResolveStatedDieCell(label, request) is { } statedDieValue
                    ? statedDieValue
                    : RequestJobFieldValues.Resolve(label, jobAvailability, AnswersWithComponent(request));

            // The scrap Item's lugger cell is pictured with the lugger's own artwork, beside the name the card
            // already spells out — the same shape the hopper card's size cell has. The picture is resolved where
            // the arrangement is in hand, so this cell carries it rather than looking one up (owner decision,
            // 2026-10-26). A lugger with no picture draws the application's one shared placeholder, exactly as a
            // size with no picture does, because a cell that names a lugger and shows nothing reads as broken.
            // A generic request's lugger picture is not the job's, so none of the job's artwork is borrowed for it.
            var isLuggerLocation = WaitlistCardCellRules.IsLuggerLocationField(label);

            return new WaitlistLineCardCell
            {
                Label = label,
                Value = jobValue ?? string.Empty,
                Order = field.Order,
                DrawsPicture = isLuggerLocation && !string.IsNullOrWhiteSpace(jobValue),
                ResolvedReferenceImagePath = isLuggerLocation && !isGenericScrap
                    ? jobAvailability?.LuggerLocationImagePath ?? string.Empty
                    : string.Empty,
            };
        }

        if (string.Equals(source, RequestItemFieldDefinition.Sources.Fixed, StringComparison.OrdinalIgnoreCase))
        {
            // A fixed field is one the request itself decides, and `Destination` is the one such field that is
            // a cell: every Deliver Item is delivered to the work centre that asked for it, so the cell reads
            // the request's own requesting work centre and nothing asks the requester where it goes (FR-042,
            // FR-057, research R1). Any other fixed label resolves nothing, so its cell is hidden without a
            // warning — the value does not exist yet rather than existing and failing to be read.
            var fixedValue = WaitlistCardCellRules.RequestWorkCentreValue(label, request);
            return new WaitlistLineCardCell { Label = label, Value = fixedValue ?? string.Empty, Order = field.Order };
        }

        WarnUnreadableDeclaredField(
            request,
            label,
            $"its declared source '{source}' is not one this build can read");

        return new WaitlistLineCardCell { Label = label, Order = field.Order };
    }

    /// <summary>
    /// The value a generic scrap request's card names for one of its two cells: the type the requester stated, or
    /// the lugger the arrangement assigns to that type right now (FR-006, FR-008, FR-009, FR-010).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A generic work center has no job, so neither fact can be read from one. The type is the wording the requester
    /// chose, kept on the request; the lugger is resolved live through the same rule set the work-order card uses,
    /// so the two cards cannot name different luggers for one type (FR-007, FR-011, FR-012).
    /// </para>
    /// <para>
    /// An empty arrangement is what a failed read looks like at this seam, and it means the lugger is unknown rather
    /// than absent, so the card says so instead of drawing nothing (FR-010). A type the arrangement knows but has no
    /// lugger for is a configuration gap: the gap is recorded and no lugger is invented (FR-009).
    /// </para>
    /// </remarks>
    /// <returns>The value to draw, or <see langword="null"/> when there is nothing true to say.</returns>
    /// <summary>
    /// The die-sourced cell of a generic die request, answered from the request's own stored answer.
    /// </summary>
    /// <param name="label">The cell's declared label.</param>
    /// <param name="request">The request, whose stored die answer is read.</param>
    /// <returns>The cell's value, or <see langword="null"/> for a label this request's answer does not own.</returns>
    /// <remarks>
    /// A generic request has no job, so a die cell that asked the job would draw nothing. The die's own answer is the
    /// only record of it, and a die whose location Visual never recorded says so rather than leaving the cell blank
    /// (020 FR-006, FR-012, SC-005).
    /// </remarks>
    private static string? ResolveStatedDieCell(string label, WaitlistRequest request)
    {
        if (!request.IsGeneric)
        {
            return null;
        }

        var stated = GenericRequestDieAnswer.TryRead(request.Answers);
        if (stated is null)
        {
            return null;
        }

        return label.Trim().ToLowerInvariant() switch
        {
            "die" => stated.DieNumber,
            "pickup location" or "die location" => stated.HasLocation
                ? stated.DieLocation
                : "Waitlist_Card_DieLocationMissing.Text".GetLocalized(),
            _ => null,
        };
    }

    private static string? ResolveStatedScrapCell(
        string label,
        WaitlistRequest request,
        LuggerArrangementSnapshot? arrangement)
    {        var stated = GenericRequestScrapAnswer.TryRead(request.Answers);
        if (stated is null)
        {
            return null;
        }

        if (WaitlistCardCellRules.IsScrapTypeField(label))
        {
            return stated.ScrapType;
        }

        if (arrangement is null || arrangement.Rows.Count == 0)
        {
            return "Waitlist_Card_LuggerUnavailable.Text".GetLocalized();
        }

        var resolution = arrangement.Resolve(stated.ScrapType);

        // The all-blocked case resolves to a lugger whose name carries its status, which is the true answer to
        // "where does this material go" and is drawn as it stands (FR-008).
        if (resolution.LuggerLocation is { } lugger)
        {
            return lugger;
        }

        if (resolution.IsConfigurationGap)
        {
            AppLog.Warn(
                "Waitlist",
                $"The scrap type '{stated.ScrapType}' stated on request {request.Id:D} has no lugger assigned, so its `Lugger Location` cell is left out. Assign the type to a lugger in the lugger arrangement so handlers know where to walk.");
        }

        return null;
    }

    /// <summary>
    /// The request's captured answers with the one answer the flow asked for added under <c>Component</c>, for an
    /// Item that names a component. The request stores that answer on itself rather than among the labelled ones,
    /// and the component's description and quantity are looked up by it.
    /// </summary>
    private static IReadOnlyDictionary<string, string>? AnswersWithComponent(WaitlistRequest request)
    {
        var item = request.Item ?? string.Empty;
        if (!item.EndsWith("-component", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(request.InputValue)
            || (request.Answers is not null && request.Answers.Keys.Any(key => string.Equals(key?.Trim(), "Component", StringComparison.OrdinalIgnoreCase))))
        {
            return request.Answers;
        }

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (request.Answers is not null)
        {
            foreach (var answer in request.Answers)
            {
                merged[answer.Key] = answer.Value;
            }
        }

        merged["Component"] = request.InputValue.Trim();
        return merged;
    }

    /// <summary>
    /// The answer a declared answer-sourced field reads.
    /// </summary>
    /// <remarks>
    /// A labelled entry the request captured under the declared label wins, because that is the request's own
    /// answer for that field. Failing that, a <b>required</b> answer-sourced field reads the one answer the flow
    /// asked for and the request stores on itself — <see cref="WaitlistRequest.Answers"/> holds only what was
    /// captured <i>beyond</i> it — which is how a one-answer Item's cell resolves. An <b>optional</b>
    /// answer-sourced field reads only its own labelled entry, so the die-shop checkbox the requester never
    /// ticked stays absent instead of reading an answer the flow asked for under another label (D40, FR-055).
    /// A required answer the request does not carry resolves to nothing, and the caller records that gap.
    /// </remarks>
    private static string? CapturedAnswer(string label, WaitlistRequest request, bool required)
    {
        if (!string.IsNullOrWhiteSpace(label) && request.Answers is { Count: > 0 } answers)
        {
            foreach (var answer in answers)
            {
                if (string.Equals(answer.Key?.Trim(), label.Trim(), StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(answer.Value))
                {
                    return answer.Value.Trim();
                }
            }
        }

        return required && !string.IsNullOrWhiteSpace(request.InputValue)
            ? request.InputValue.Trim()
            : null;
    }

    /// <summary>
    /// Records a declared field whose source exists but cannot be read, so the gap can be chased up later
    /// (FR-058, FR-101, D41). It is never drawn as a reason on the card and never filled with a substitute.
    /// </summary>
    private static void WarnUnreadableDeclaredField(WaitlistRequest request, string label, string reason) =>
        AppLog.Warn(
            "Waitlist",
            $"The card for request {request.Id:D} (item '{request.Item}') cannot read its declared field '{label}': {reason}. The cell is hidden.");

    /// <summary>
    /// Human-friendly waiting-age for a request, e.g. "Waiting 35m" / "Waiting 1h 20m" / "Waiting 2d",
    /// so leads can prioritize the oldest requests first.
    /// </summary>
    /// <remarks>
    /// The formatting itself lives on <see cref="SampleOrder"/> because the cards re-render these texts every
    /// minute; this is the call site the row factory and the tests use.
    /// </remarks>
    public static string GetWaitingForText(DateTimeOffset requestedUtc, DateTimeOffset? referenceUtc = null) =>
        SampleOrder.FormatWaitingAge(requestedUtc, referenceUtc);

    private static string GetRemainingTimeText(DateTimeOffset? targetTimeUtc, bool isOverdue) =>
        SampleOrder.FormatRemainingTime(targetTimeUtc, isOverdue);

    private static void AddRequestFields(SampleOrder item, WaitlistRequest request)
    {
        // Every row below is sourced from the request itself. Nothing is derived from a retired type/subtype
        // string and nothing is invented for a value the request does not carry: an Item's own attributes are
        // restored by the item-resolution work, and until then this surface states only what it can support
        // (FR-001, FR-002). The two-line identity of the request travels on the card, not in these slots.
        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                item.Fields.Add(new WaitlistField { Label = label, Value = value.Trim() });
            }
        }

        Add("Request details", request.InputValue);

        // A generic request has no work order anywhere, so this row is not drawn for it rather than drawn blank
        // (FR-008, FR-025). A work-order request's row keeps its place and its order.
        if (!request.IsGeneric)
        {
            Add("Work order", request.ActiveSetupJobId);
        }

        Add("Work center", request.WorkCenter);
        Add("Requested by", request.RequesterEmployeeName);
        Add("Request ID", request.Id.ToString("N"));
    }
}
