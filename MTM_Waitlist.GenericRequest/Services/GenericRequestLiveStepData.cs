using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.GenericRequest.Services;

/// <summary>
/// The live answer behind every generic request screen: configured work centers, categories and items read from the
/// stores the work-order wizard already uses, and one request raised through the same write door as every other.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here holds a value of its own. A read either asks a store and reports what came back, or reports that the
/// store could not be reached, which is what keeps a generic request as truthful as a work-order one (FR-028).
/// </para>
/// <para>
/// <b>The four externally proved answers are not implemented here yet.</b> A part is proved against Infor Visual and
/// the WIP floor (chunk 017), dunnage comes from the dunnage catalogue (018), a scrap type from the plant's own list
/// (019) and a die from a Visual search (020). Each read answers <see cref="GenericRequestStepResult{T}.Unavailable"/>
/// so its step reports the source as not available and offers a retry, and no substitute is offered (FR-012).
/// </para>
/// </remarks>
public sealed class GenericRequestLiveStepData : IGenericRequestStepData
{
    private const string LogCategory = "WaitlistGenericRequest";

    private readonly IWorkCenterCatalogService _workCenterCatalogService;
    private readonly INewRequestPickerService _pickerService;
    private readonly IRequestItemConfigurationService _itemConfigurationService;
    private readonly IWaitlistRequestService _requestService;
    private readonly IPersonIdentity _personIdentity;
    private readonly IEmployeeDirectoryService _employeeDirectoryService;

    public GenericRequestLiveStepData(
        IWorkCenterCatalogService workCenterCatalogService,
        INewRequestPickerService pickerService,
        IRequestItemConfigurationService itemConfigurationService,
        IWaitlistRequestService requestService,
        IPersonIdentity personIdentity,
        IEmployeeDirectoryService employeeDirectoryService)
    {
        _workCenterCatalogService = workCenterCatalogService ?? throw new ArgumentNullException(nameof(workCenterCatalogService));
        _pickerService = pickerService ?? throw new ArgumentNullException(nameof(pickerService));
        _itemConfigurationService = itemConfigurationService ?? throw new ArgumentNullException(nameof(itemConfigurationService));
        _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
        _personIdentity = personIdentity ?? throw new ArgumentNullException(nameof(personIdentity));
        _employeeDirectoryService = employeeDirectoryService ?? throw new ArgumentNullException(nameof(employeeDirectoryService));
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestWorkCenterOption>> ReadWorkCentersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var catalog = await _workCenterCatalogService
                .GetCatalogAsync(_workCenterCatalogService.GetCurrentComputerName(), cancellationToken)
                .ConfigureAwait(false);

            var options = catalog.WorkCenterDetails
                .Where(detail => detail.Value.IsGeneric)
                .OrderBy(detail => detail.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(detail => new GenericRequestWorkCenterOption(detail.Key, detail.Value.Building))
                .ToArray();

            return options.Length == 0
                ? GenericRequestStepResult<GenericRequestWorkCenterOption>.Empty(DateTimeOffset.UtcNow)
                : GenericRequestStepResult<GenericRequestWorkCenterOption>.Populated(options, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Reading the generic work centers failed.");
            return GenericRequestStepResult<GenericRequestWorkCenterOption>.Unavailable(DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestCategoryOption>> ReadCategoriesAsync(
        string workCenter,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;

        var offered = GenericRequestCategoryCatalog.All()
            .Where(category => TryResolveCategory(category.Code, out var requestCategory)
                && _pickerService.GetItems(requestCategory).Count > 0)
            .ToArray();

        return Task.FromResult(
            offered.Length == 0
                ? GenericRequestStepResult<GenericRequestCategoryOption>.Empty(attemptedAtUtc)
                : GenericRequestStepResult<GenericRequestCategoryOption>.Populated(offered, attemptedAtUtc));
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestItemOption>> ReadItemsAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveCategory(category, out var requestCategory))
        {
            return GenericRequestStepResult<GenericRequestItemOption>.Empty(DateTimeOffset.UtcNow);
        }

        var definitions = _pickerService.GetItems(requestCategory);
        if (definitions.Count == 0)
        {
            return GenericRequestStepResult<GenericRequestItemOption>.Empty(DateTimeOffset.UtcNow);
        }

        RequestItemConfigurationSet configurations;
        try
        {
            configurations = await _itemConfigurationService
                .GetConfigurationsAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, $"Reading the item configurations for '{category}' failed.");
            return GenericRequestStepResult<GenericRequestItemOption>.Unavailable(DateTimeOffset.UtcNow);
        }

        // An item whose configuration cannot be read cannot be offered, because the flow would then present it
        // without the question it was supposed to ask. The whole step reports unavailable rather than quietly
        // dropping the item, so the requester is told the source is not usable (FR-021, FR-029).
        foreach (var definition in definitions)
        {
            if (!configurations.Contains(definition.Id))
            {
                AppLog.Info(LogCategory, $"Item '{definition.Id}' has no usable configuration, so the item step cannot be offered.");
                return GenericRequestStepResult<GenericRequestItemOption>.Unavailable(DateTimeOffset.UtcNow);
            }
        }

        var options = definitions
            .Select(definition => ToOption(definition, configurations.Get(definition.Id)))
            .ToArray();

        return GenericRequestStepResult<GenericRequestItemOption>.Populated(options, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestPartOption>> ReadPartsAsync(
        string itemCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestPartOption>.Unavailable(DateTimeOffset.UtcNow));

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
        string partNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(DateTimeOffset.UtcNow));

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestDunnageOption>> ReadDunnageAsync(
        string itemCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestDunnageOption>.Unavailable(DateTimeOffset.UtcNow));

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestScrapOption>> ReadScrapTypesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestScrapOption>.Unavailable(DateTimeOffset.UtcNow));

    /// <inheritdoc />
    public Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(
        string searchText,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericRequestStepResult<GenericRequestDieOption>.Unavailable(DateTimeOffset.UtcNow));

    /// <summary>
    /// Whether a step that reads no rows can be shown.
    /// </summary>
    /// <remarks>
    /// The four externally proved answers report unavailable because their source has not shipped. Every other step
    /// has something to show: its own answer fields. The result step's presentation comes from the raise attempt the
    /// summary recorded, not from here.
    /// </remarks>
    public Task<GenericRequestStepAvailability> ReadStepAvailabilityAsync(
        GenericRequestStep step,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(step switch
            {
                GenericRequestStep.Part
                    or GenericRequestStep.Dunnage
                    or GenericRequestStep.Scrap
                    or GenericRequestStep.Die => GenericRequestStepAvailability.Unavailable(DateTimeOffset.UtcNow),
                _ => GenericRequestStepAvailability.Ready(DateTimeOffset.UtcNow),
            });

    /// <inheritdoc />
    public async Task<GenericRequestSubmission> SubmitAsync(
        GenericRequestDraft draft,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;

        if (draft is null)
        {
            return GenericRequestSubmission.Failed("A request has to be described before it can be raised.", attemptedAtUtc);
        }

        try
        {
            // The requester is the signed-in person and nobody else: the identity comes from the session and the
            // account records, never from anything the draft carries (FR-020).
            var employeeNumber = _personIdentity.EmployeeNumber ?? string.Empty;
            var storedEmployee = await _employeeDirectoryService
                .FindByEmployeeIdentifierAsync(employeeNumber, cancellationToken)
                .ConfigureAwait(false);
            var verification = NewRequestFlowRules.VerifyEmployeeIdentity(employeeNumber, storedEmployee);

            if (!verification.IsValid)
            {
                return GenericRequestSubmission.Failed(verification.Message, attemptedAtUtc);
            }

            var result = await _requestService
                .SubmitAsync(
                    new WaitlistRequestDraft
                    {
                        Building = draft.Building,
                        WorkCenter = draft.WorkCenter,
                        Category = draft.Category,
                        Item = draft.ItemCode,
                        InputValue = string.IsNullOrWhiteSpace(draft.Answer) ? null : draft.Answer,
                        IsGeneric = true,
                        Description = draft.Description,
                        AcceptedWarnings = draft.AcceptedWarnings.Count == 0 ? null : draft.AcceptedWarnings.ToArray(),
                        WorkCenterName = _workCenterCatalogService.GetCurrentComputerName(),
                        RequesterEmployeeNumber = verification.EmployeeNumber,
                        RequesterEmployeeName = verification.EmployeeName,
                        RequestedUtc = attemptedAtUtc,
                        TargetTimeUtc = draft.NeededBy,
                        IsOverdue = false,
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.Status == WaitlistRequestSubmitStatus.Success && result.Request is { } raised)
            {
                AppLog.Info(LogCategory, $"Raised generic request '{raised.Id}' for work center '{draft.WorkCenter}', item '{draft.ItemCode}'.");
                return GenericRequestSubmission.Raised(raised.Id.ToString(), attemptedAtUtc);
            }

            AppLog.Info(LogCategory, $"The generic request was not raised: {result.Status} - {result.Message}");
            return GenericRequestSubmission.Failed(result.Message, attemptedAtUtc);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, "Raising the generic request failed.");
            return GenericRequestSubmission.Failed(
                "The request could not be raised because the queue could not be reached. Try again.",
                attemptedAtUtc);
        }
    }

    /// <summary>
    /// The category option for one catalogue definition, with every answer the item's own configuration declares.
    /// </summary>
    private static GenericRequestItemOption ToOption(RequestItemDefinition definition, RequestItemConfiguration configuration)
    {
        var (needsPart, needsDunnage, needsScrap, needsDie) = RequirementsFor(definition);

        var answerKind = configuration.RequiresAnswer
            ? configuration.AnswerValueType switch
            {
                RequestItemValueType.Enum => GenericRequestAnswerKind.Choice,
                _ => GenericRequestAnswerKind.Text,
            }
            : GenericRequestAnswerKind.None;

        return new GenericRequestItemOption(
            Code: definition.Id,
            Name: RequestItemCatalog.ResolveItemStepLabel(definition),
            NeedsPart: needsPart,
            NeedsDunnage: needsDunnage,
            NeedsScrap: needsScrap,
            NeedsDie: needsDie,
            PromptText: configuration.PromptText ?? string.Empty,
            AnswerKind: answerKind,
            AnswerOptions: answerKind == GenericRequestAnswerKind.Choice ? configuration.Options : Array.Empty<string>(),
            MinLength: configuration.MinLength,
            MaxLength: configuration.MaxLength,
            AllottedMinutes: configuration.AllottedMinutes);
    }

    /// <summary>
    /// Which answer an item's request has to state, read from the token its own card line declares.
    /// </summary>
    /// <remarks>
    /// The catalogue already says what each item's request is about: a <c>{part_number}</c> or <c>{component}</c> line
    /// names a proved part, <c>{die}</c> a die, <c>{dunnage_part}</c> a dunnage part and <c>{scrap_type}</c> a scrap
    /// type. A literal line, or <c>{answer}</c>, names none of the four, so that item's request is answered by its
    /// configured question and nothing else. Reading the declaration here is what keeps the flow's shape out of a
    /// second list in code (research R9).
    /// </remarks>
    private static (bool NeedsPart, bool NeedsDunnage, bool NeedsScrap, bool NeedsDie) RequirementsFor(RequestItemDefinition definition)
    {
        var template = definition.CardLine2Template ?? string.Empty;

        if (template.Contains("{die}", StringComparison.OrdinalIgnoreCase))
        {
            return (false, false, false, true);
        }

        if (template.Contains("{dunnage_part}", StringComparison.OrdinalIgnoreCase))
        {
            return (false, true, false, false);
        }

        if (template.Contains("{scrap_type}", StringComparison.OrdinalIgnoreCase))
        {
            return (false, false, true, false);
        }

        if (template.Contains("{part_number}", StringComparison.OrdinalIgnoreCase)
            || template.Contains("{component}", StringComparison.OrdinalIgnoreCase))
        {
            return (true, false, false, false);
        }

        return (false, false, false, false);
    }

    /// <summary>
    /// The catalogue category an offered category code names.
    /// </summary>
    private static bool TryResolveCategory(string code, out RequestCategory category)
    {
        foreach (var candidate in Enum.GetValues<RequestCategory>())
        {
            if (string.Equals(candidate.ToString(), code?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                category = candidate;
                return true;
            }
        }

        category = default;
        return false;
    }
}
