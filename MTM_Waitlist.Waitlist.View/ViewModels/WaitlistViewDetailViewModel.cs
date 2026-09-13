using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly IWaitlistRequestService? _requestService;
    private readonly IWaitlistInventoryService? _inventoryService;
    private readonly IImageLocationService? _imageLocationService;
    private IDisposable? _imageLocationSubscription;
    private int? _lastOrderId;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

    /// <summary>
    /// Caption shown under the remaining-time value on the details page.
    /// </summary>
    /// <remarks>
    /// This view model builds its row once in <see cref="OnNavigatedTo"/> and owns no dispatcher tick, so
    /// unlike the list card this countdown does not advance while the page is open. The caption says so
    /// instead of letting the number look live.
    /// </remarks>
    public string RemainingTimeNoteText => "Waitlist_Detail.RemainingTimeNote".GetLocalized();

    [ObservableProperty]
    public partial string EmptyStateMessage
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmptyStateVisible
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsItemPresent
    {
        get; set;
    }

    /// <summary>
    /// True when a section's read threw or reported an error. The failure is rendered in place, with
    /// <see cref="RetryLoadCommand"/>, and is never rendered as absence and never as the page-level store
    /// outage (<c>data-model.md</c> §1 invariants 1–3).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSectionLoadFailed
    {
        get; set;
    }

    /// <summary>Localized explanation shown in place of the sections whose read failed.</summary>
    public string SectionFailureMessage => "Waitlist_Detail.BlockFailed.Message".GetLocalized();

    /// <summary>Localized label for the section-failure retry action.</summary>
    public string SectionFailureRetryText => "Waitlist_Detail.BlockFailed.Retry".GetLocalized();

    /// <summary>
    /// Re-runs the read that failed and rebuilds the sections. Bound to the retry action on the failure
    /// block, so a failure is recoverable without leaving the page.
    /// </summary>
    [RelayCommand]
    private void RetryLoad()
    {
        LoadItemAndSections();

        if (Item is not null && _inventoryService is not null)
        {
            var inventoryPart = ResolveInventoryPartNumber(Item);
            if (!string.IsNullOrWhiteSpace(inventoryPart))
            {
                _ = LoadInventoryAsync(inventoryPart);
            }
        }
    }

    /// <summary>
    /// True when the location grid has no rows to show (no part resolved, service absent, or
    /// every row was filtered by the on-hand &gt;= 1 / ignored-location rules).
    /// </summary>
    [ObservableProperty]
    public partial bool IsInventoryEmpty
    {
        get; set;
    } = true;

    public ObservableCollection<InventoryLocationRow> InventoryRows { get; } = new();

    public ICommand? SortInventoryCommand
    {
        get;
        private set;
    }

    private string? _inventorySortColumn;
    private bool _inventorySortDescending;

    public ObservableCollection<WaitlistDetailTemplateSection> TemplateSections { get; } = new();

    public WaitlistViewDetailViewModel(
        INavigationService navigationService,
        IBuildingSelectionService buildingSelectionService,
        IImageLocationService? imageLocationService = null,
        IWaitlistRequestService? requestService = null,
        IWaitlistInventoryService? inventoryService = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _navigationService = navigationService;
        _buildingSelectionService = buildingSelectionService;
        _imageLocationService = imageLocationService;
        _requestService = requestService;
        _inventoryService = inventoryService;
        SortInventoryCommand = new RelayCommand<string>(SortInventoryBy);
    }

    /// <summary>
    /// Re-sorts <see cref="InventoryRows"/> by a column (PartNumber / Quantity / Location).
    /// Clicking the same column again toggles ascending/descending; default ascending.
    /// </summary>
    public void SortInventoryBy(string? column)
    {
        var normalized = (column ?? string.Empty).Trim();
        if (normalized.Length == 0 || InventoryRows.Count == 0)
        {
            return;
        }

        var descending = string.Equals(_inventorySortColumn, normalized, StringComparison.OrdinalIgnoreCase)
            ? !_inventorySortDescending
            : false;
        _inventorySortColumn = normalized;
        _inventorySortDescending = descending;

        IEnumerable<InventoryLocationRow> sorted = normalized.ToLowerInvariant() switch
        {
            "partnumber" => descending
                ? InventoryRows.OrderByDescending(row => row.PartNumber, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Location, StringComparer.OrdinalIgnoreCase)
                : InventoryRows.OrderBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Location, StringComparer.OrdinalIgnoreCase),
            "quantity" => descending
                ? InventoryRows.OrderByDescending(row => row.OnHandQuantity)
                : InventoryRows.OrderBy(row => row.OnHandQuantity),
            _ => descending
                ? InventoryRows.OrderByDescending(row => row.Location, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase)
                : InventoryRows.OrderBy(row => row.Location, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.PartNumber, StringComparer.OrdinalIgnoreCase),
        };

        // Materialize BEFORE clearing the source collection (OrderBy is deferred).
        var materialized = sorted.ToArray();
        InventoryRows.Clear();
        foreach (var row in materialized)
        {
            InventoryRows.Add(row);
        }

        StartupDebugLog.Info("WaitlistDetail", $"SortInventoryBy '{normalized}' descending={descending}. Count={InventoryRows.Count}.");
    }
    public void OnNavigatedTo(object parameter)
    {
        _lastOrderId = parameter switch
        {
            int intId => intId,
            long longId when longId <= int.MaxValue && longId >= int.MinValue => (int)longId,
            _ => (int?)null
        };

        if (_imageLocationSubscription is null
            && _imageLocationService is not null
            && _imageLocationService.IsInitialized)
        {
            _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
        }

        LoadItemAndSections();

        if (_inventoryService is not null && Item is not null)
        {
            var inventoryPart = ResolveInventoryPartNumber(Item);
            if (!string.IsNullOrWhiteSpace(inventoryPart))
            {
                _ = LoadInventoryAsync(inventoryPart);
            }
        }
    }

    /// <summary>
    /// Resolves the request row and rebuilds the sections as one unit, so a read that throws produces the
    /// Failed block state rather than a half-built page or a failure disguised as absence.
    /// </summary>
    private void LoadItemAndSections()
    {
        try
        {
            ResolveItem();
            LoadTemplateSections();
            IsSectionLoadFailed = false;
        }
        catch (Exception ex)
        {
            Item = null;
            TemplateSections.Clear();
            IsSectionLoadFailed = true;
            IsItemPresent = false;
            IsEmptyStateVisible = false;
            EmptyStateMessage = string.Empty;
            StartupDebugLog.Error(
                "WaitlistDetail",
                ex,
                "The request read behind the detail sections failed; the failure is rendered in place with a retry.");
        }
    }

    /// <summary>
    /// Resolves the request on screen. The detail page resolves only real submitted requests (the list
    /// surfaces each request as a <see cref="SampleOrder"/> whose Id is the hash of the request Guid).
    /// </summary>
    private void ResolveItem()
    {
        Item = null;

        if (_lastOrderId is int orderId && _requestService is not null)
        {
            var requests = _requestService.GetActiveRequests(_buildingSelectionService.SelectedBuilding);
            var match = requests.FirstOrDefault(request => request.Id.GetHashCode() == orderId);
            if (match is not null)
            {
                Item = WaitlistViewViewModel.CreateSessionOrder(match);
            }
        }

        IsItemPresent = Item is not null;
        IsEmptyStateVisible = Item is null;
        EmptyStateMessage = Item is null
            ? "No waitlist request or coil details are available to show."
            : string.Empty;
    }

    public void OnNavigatedFrom()
    {
        _imageLocationSubscription?.Dispose();
        _imageLocationSubscription = null;
    }

    /// <summary>The coil part keyed in the receiving_history seed for the sample coil (MMC0001000).</summary>
    private const string CoilReceivingPartId = "MMC0001000";

    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
    }

    private void LoadTemplateSections()
    {
        TemplateSections.Clear();

        if (Item is null)
        {
            return;
        }

        switch (Item.ImagePath.Trim().ToLowerInvariant())
        {
            case "pickup_fg.png":
                LoadFinishedGoodsSections(Item);
                break;
            case "pickup_ncm.png":
                LoadNcmSections(Item);
                break;
            case "pickup_os.png":
                LoadOutsideServiceSections(Item);
                break;
            case "pickup_wip.png":
                LoadWipSections(Item);
                break;
            case "scrap.png":
                LoadScrapSections(Item);
                break;
            default:
                LoadCoilSections(Item);
                break;
        }
    }

    private void LoadCoilSections(SampleOrder item)
    {
        var request = ResolveRequest(item);

        AddSection(
            "Coil material",
            "Material information carried by the request itself.",
            ("Subtype", FieldValue(item, "Subtype")),
            ("Request details", FieldValue(item, "Request details")),
            ("Requesting work center", FieldValue(item, "Requesting work center")));

        AddSection(
            "Work order and request",
            "Request ownership and work-order context for the coil movement.",
            ("Work order", WorkOrderText(item, request)),
            ("Work center", item.RequestedPressName),
            ("Requesting user", item.RequestedByName),
            ("Employee number", EmployeeNumberText(request, item)),
            ("Remaining time", item.RemainingTimeText));
    }

    private void LoadFinishedGoodsSections(SampleOrder item)
    {
        AddSection(
            "Customer order",
            "Customer and part information for the finished-goods pickup.",
            ("Subtype", FieldValue(item, "Subtype")),
            ("Request details", FieldValue(item, "Request details")),
            ("Part number", FieldValue(item, "Part number")),
            ("Part description", FieldValue(item, "Part description")),
            ("Customer", FieldValue(item, "Customer")),
            ("Packlist", FieldValue(item, "Packlist")),
            ("Quantity remaining", FieldValue(item, "Quantity remaining")));

        AddRequestContextSection(item, "Finished-goods workflow", "Confirm assignment, pickup, and shipment status before closing the request.");
    }

    private void LoadNcmSections(SampleOrder item)
    {
        AddSection(
            "NCM pickup",
            "Material-handler information for moving nonconforming material to the NCM area.",
            ("Subtype", FieldValue(item, "Subtype")),
            ("Request details", FieldValue(item, "Request details")),
            ("Pickup location", FieldValue(item, "Pickup location")));

        AddRequestContextSection(item, "NCM workflow", "Record handler pickup, NCM-area delivery, Quality ownership, and disposition approval.");
    }

    private void LoadOutsideServiceSections(SampleOrder item)
    {
        AddSection(
            "Pickup and delivery",
            "Material-handler instructions for moving material from the work center to outside service.",
            ("Subtype", FieldValue(item, "Subtype")),
            ("Request details", FieldValue(item, "Request details")),
            ("Pickup work center", FieldValue(item, "Pickup work center")));

        AddRequestContextSection(item, "Outside-service workflow", "Record handler pickup, delivery acknowledgement, service status, and return tracking.");
    }

    private void LoadWipSections(SampleOrder item)
    {
        AddSection(
            "WIP pickup and inventory",
            "Material-handler instructions for moving WIP from the work center into the assigned WIP location.",
            ("Subtype", FieldValue(item, "Subtype")),
            ("Work order", FieldValue(item, "Work order")),
            ("Request details", FieldValue(item, "Request details")),
            ("Pickup work center", FieldValue(item, "Pickup work center")));

        AddRequestContextSection(item, "WIP workflow", "Record pickup acknowledgement, inventory transaction, destination confirmation, and handler.");
    }

    private void LoadScrapSections(SampleOrder item)
    {
        AddSection(
            "Scrap pickup and lugger",
            "Material-handler instructions for moving scrap to the correct lugger.",
            ("Scrap lugger", FieldValue(item, "Scrap lugger")),
            ("Pickup work center", FieldValue(item, "Pickup work center")),
            ("Request details", FieldValue(item, "Request details")));

        AddRequestContextSection(item, "Scrap workflow", "Record handler pickup, lugger placement, confirmation, and any correction to the selected category.");
    }

    private void AddRequestContextSection(SampleOrder item, string title, string summary)
        => AddSection(
            title,
            summary,
            ("Requested by", item.RequestedByName),
            ("Press or resource", item.RequestedPressName),
            ("Remaining time", item.RemainingTimeText));

    /// <summary>
    /// The label-to-value lookup used by every section loader. It has no fallback: a label with no source
    /// returns <see langword="null" /> and its row is not rendered (FR-002, contract C2).
    /// </summary>
    private static string? FieldValue(SampleOrder item, string label)
        => item.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>
    /// Resolves the underlying <see cref="WaitlistRequest"/> for the row on screen (via its
    /// <see cref="SampleOrder.RequestId"/>), or null when the row carries no request id or the service is absent.
    /// </summary>
    private WaitlistRequest? ResolveRequest(SampleOrder item)
    {
        if (item.RequestId is Guid requestId && _requestService is not null)
        {
            return _requestService.GetRequest(requestId);
        }

        return null;
    }

    /// <summary>Work-order/job context for the request: a 'Work order' field when present, else the active job id.</summary>
    private static string? WorkOrderText(SampleOrder item, WaitlistRequest? request)
    {
        var fieldValue = item.Fields
            .FirstOrDefault(field => string.Equals(field.Label, "Work order", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        if (!string.IsNullOrWhiteSpace(fieldValue))
        {
            return fieldValue;
        }

        var jobId = request?.ActiveSetupJobId;
        return string.IsNullOrWhiteSpace(jobId) ? null : jobId;
    }

    /// <summary>Requester employee number for the request, or null when none is known.</summary>
    private static string? EmployeeNumberText(WaitlistRequest? request, SampleOrder item)
    {
        var employeeNumber = request is not null && !string.IsNullOrWhiteSpace(request.RequesterEmployeeNumber)
            ? request.RequesterEmployeeNumber
            : item.RequesterEmployeeNumber;
        return string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber;
    }

    /// <summary>
    /// Adds a section, dropping every row whose value has no source and the section itself when no row
    /// survives. A block with nothing to show is hidden rather than drawn as an empty titled shell
    /// (<c>data-model.md</c> §1, invariant 2).
    /// </summary>
    private void AddSection(string title, string summary, params (string Label, string? Value)[] fields)
    {
        var section = new WaitlistDetailTemplateSection
        {
            Title = title,
            Summary = summary
        };

        foreach (var (label, value) in fields)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                section.Fields.Add(new WaitlistDetailTemplateField
                {
                    Label = label,
                    Value = value.Trim()
                });
            }
        }

        if (section.Fields.Count > 0)
        {
            TemplateSections.Add(section);
        }
    }

    /// <summary>
    /// Loads the filtered inventory-location rows for a part into <see cref="InventoryRows"/>.
    /// </summary>
    public async Task LoadInventoryAsync(string? partNumber, CancellationToken cancellationToken = default)
    {
        InventoryRows.Clear();
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPart) || _inventoryService is null)
        {
            IsInventoryEmpty = true;
            return;
        }

        var rows = await _inventoryService.GetInventoryLocationRowsAsync(normalizedPart, cancellationToken).ConfigureAwait(false);
        foreach (var row in rows)
        {
            InventoryRows.Add(row);
        }

        IsInventoryEmpty = InventoryRows.Count == 0;
        StartupDebugLog.Info("WaitlistDetail", $"LoadInventoryAsync completed. Part='{normalizedPart}', Rows={InventoryRows.Count}.");
    }

    /// <summary>
    /// Best-effort part/inventory token for an order: prefers a real Part number/Part field,
    /// then the coil's "Requested coil" identifier, so the coil detail grid has something to
    /// query (mock inventory is keyed per part token).
    /// </summary>
    public static string? ResolveInventoryPartNumber(SampleOrder? item)
    {
        if (item is null)
        {
            return null;
        }

        foreach (var label in new[] { "Part number", "Part", "Requested coil" })
        {
            var candidate = FieldValue(item, label)?.Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void OnImageLocationChanged(ImageLocationChangedEventArgs args)
    {
        if (Item is null || _imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return;
        }

        _ = RefreshResolvedPathsAsync(Item);
    }

    private async Task RefreshResolvedPathsAsync(SampleOrder item)
    {
        if (_imageLocationService is null || !_imageLocationService.IsInitialized)
        {
            return;
        }

        if (item.SubtypeStableId.HasValue)
        {
            item.ResolvedImagePath = await _imageLocationService
                .ResolveRequestSubtypeImagePathAsync(item.SubtypeStableId.Value.ToString())
                .ConfigureAwait(false);
        }
        else if (item.RequestTypeStableId.HasValue)
        {
            item.ResolvedImagePath = await _imageLocationService
                .ResolveRequestTypeImagePathAsync(item.RequestTypeStableId.Value.ToString())
                .ConfigureAwait(false);
        }

        if (item.WorkCenterCatalogId.HasValue)
        {
            item.WorkCenterImagePath = await _imageLocationService
                .ResolveWorkCenterImagePathAsync(item.WorkCenterCatalogId.Value.ToString())
                .ConfigureAwait(false);
        }

        OnPropertyChanged(nameof(Item));
    }
}
