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
    private readonly IAverageCoilWeightService? _averageCoilWeightService;
    private IDisposable? _imageLocationSubscription;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

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
        IWaitlistInventoryService? inventoryService = null,
        IAverageCoilWeightService? averageCoilWeightService = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _navigationService = navigationService;
        _buildingSelectionService = buildingSelectionService;
        _imageLocationService = imageLocationService;
        _requestService = requestService;
        _inventoryService = inventoryService;
        _averageCoilWeightService = averageCoilWeightService;
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
        var orderId = parameter switch
        {
            int intId => intId,
            long longId when longId <= int.MaxValue && longId >= int.MinValue => (int)longId,
            _ => (int?)null
        };

        if (orderId.HasValue && _requestService is not null)
        {
            // The detail page resolves only real submitted requests (the list surfaces each request as a
            // SampleOrder whose Id is the hash of the request Guid).
            var requests = _requestService.GetActiveRequests(_buildingSelectionService.SelectedBuilding);
            var match = requests.FirstOrDefault(request => request.Id.GetHashCode() == orderId.Value);
            if (match is not null)
            {
                Item = WaitlistViewViewModel.CreateSessionOrder(match);
            }
        }

        EmptyStateMessage = Item is null
            ? "No waitlist request or coil details are available to show."
            : string.Empty;
        IsEmptyStateVisible = Item is null;
        IsItemPresent = Item is not null;

        if (_imageLocationSubscription is null
            && _imageLocationService is not null
            && _imageLocationService.IsInitialized)
        {
            _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
        }

        LoadTemplateSections();
        _ = EnrichCoilAverageWeightAsync();

        if (_inventoryService is not null && Item is not null)
        {
            var inventoryPart = ResolveInventoryPartNumber(Item);
            if (!string.IsNullOrWhiteSpace(inventoryPart))
            {
                _ = LoadInventoryAsync(inventoryPart);
            }
        }
    }

    public void OnNavigatedFrom()
    {
        _imageLocationSubscription?.Dispose();
        _imageLocationSubscription = null;
    }

    /// <summary>
    /// For a coil request card, replace the baked-in "Average coil weight" with the value pulled
    /// from <c>mtm_receiving_application.receiving_history</c> (RecvMockData ON = sample, OFF =
    /// real query) and rebuild the sections so the UI reflects it.
    /// </summary>
    private async Task EnrichCoilAverageWeightAsync()
    {
        if (_averageCoilWeightService is null || Item is null)
        {
            return;
        }

        // Only coil cards carry an "Average coil weight" field; skip every other request type.
        var field = Item.Fields.FirstOrDefault(f => string.Equals(f.Label, "Average coil weight", StringComparison.Ordinal));
        if (field is null)
        {
            return;
        }

        var resolved = await _averageCoilWeightService.ResolveAverageCoilWeightTextAsync(CoilReceivingPartId).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(resolved) || string.Equals(resolved, field.Value, StringComparison.Ordinal))
        {
            return;
        }

        field.Value = resolved;
        LoadTemplateSections();
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
        TemplateSections.Add(CreateTemplateSection(
            "Coil material",
            "Material and inventory information needed to select and stage the requested coil.",
            ("Requested coil", FieldValue(item, "Requested coil")),
            ("Quantity in house", FieldValue(item, "Quantity in house")),
            ("Description", FieldValue(item, "Coil description")),
            ("Average weight", FieldValue(item, "Average coil weight"))));

        TemplateSections.Add(CreateTemplateSection(
            "Work order and request",
            "Request ownership and work-order context for the coil movement.",
            ("Work order", WorkOrderText(item, request)),
            ("Work center", FieldValue(item, "Requesting work center", item.RequestedPressName)),
            ("Requesting user", item.RequestedByName),
            ("Employee number", EmployeeNumberText(request, item))));

        TemplateSections.Add(CreateTemplateSection(
            "Handling",
            "Confirm the equipment and timing required to move the coil safely.",
            ("Tipping strategy", "Crane below 10 in; otherwise Tipper"),
            ("Press", item.RequestedPressName),
            ("Remaining time", item.RemainingTimeText)));
    }

    private void LoadFinishedGoodsSections(SampleOrder item)
    {
        TemplateSections.Add(CreateTemplateSection(
            "Customer order",
            "Customer and part information for the finished-goods pickup.",
            ("Customer", FieldValue(item, "Customer")),
            ("Part number", FieldValue(item, "Part number")),
            ("Description", FieldValue(item, "Part description")),
            ("Order number", "Not available")));

        TemplateSections.Add(CreateTemplateSection(
            "Shipment",
            "Shipment identifiers and delivery timing for the requested finished goods.",
            ("Packlist", FieldValue(item, "Packlist")),
            ("Ship via", "Not available"),
            ("Expected delivery", "Not available"),
            ("Remaining quantity", FieldValue(item, "Quantity remaining"))));

        AddRequestContextSection(item, "Finished-goods workflow", "Confirm assignment, pickup, and shipment status before closing the request.");
    }

    private void LoadNcmSections(SampleOrder item)
    {
        TemplateSections.Add(CreateTemplateSection(
            "NCM pickup",
            "Material-handler information for moving nonconforming material to the NCM area.",
            ("Part", FieldValue(item, "Part")),
            ("Quantity to move", FieldValue(item, "Quantity to move")),
            ("Pickup location", FieldValue(item, "Pickup location")),
            ("Destination", FieldValue(item, "Destination")),
            ("Traceability", FieldValue(item, "Traceability ID"))));

        TemplateSections.Add(CreateTemplateSection(
            "Quality review",
            "Quality must be able to identify the defect, contain affected material, and record its disposition.",
            ("Nonconformance", "Not available"),
            ("Containment", "Not available"),
            ("Inspection status", "Pending"),
            ("Disposition", "Pending Quality review")));

        AddRequestContextSection(item, "NCM workflow", "Record handler pickup, NCM-area delivery, Quality ownership, and disposition approval.");
    }

    private void LoadOutsideServiceSections(SampleOrder item)
    {
        TemplateSections.Add(CreateTemplateSection(
            "Pickup and delivery",
            "Material-handler instructions for moving material from the work center to outside service.",
            ("Part or work order", FieldValue(item, "Part or work order")),
            ("Quantity to move", FieldValue(item, "Quantity to move")),
            ("Pickup work center", FieldValue(item, "Pickup work center")),
            ("Destination", FieldValue(item, "Outside-service destination"))));

        TemplateSections.Add(CreateTemplateSection(
            "Outside service",
            "Vendor and operation information needed to dispatch and track the service step.",
            ("Vendor or service", FieldValue(item, "Vendor or service")),
            ("Operation sequence", "Not available"),
            ("Dispatch status", "Pending pickup"),
            ("Expected return", "Not available")));

        AddRequestContextSection(item, "Outside-service workflow", "Record handler pickup, delivery acknowledgement, service status, and return tracking.");
    }

    private void LoadWipSections(SampleOrder item)
    {
        TemplateSections.Add(CreateTemplateSection(
            "WIP pickup and inventory",
            "Material-handler instructions for moving WIP from the work center into the assigned WIP location.",
            ("Work order", FieldValue(item, "Work order")),
            ("Part and quantity", FieldValue(item, "Part and quantity")),
            ("Pickup work center", FieldValue(item, "Pickup work center")),
            ("WIP destination", FieldValue(item, "WIP destination"))));

        TemplateSections.Add(CreateTemplateSection(
            "Work order and operation",
            "Work-order and operation context used to keep inventoried WIP connected to the job.",
            ("Operation sequence", FieldValue(item, "Operation sequence")),
            ("Operation status", "Not available"),
            ("Quantity still needed", "Not available"),
            ("Scheduled finish", "Not available")));

        AddRequestContextSection(item, "WIP workflow", "Record pickup acknowledgement, inventory transaction, destination confirmation, and handler.");
    }

    private void LoadScrapSections(SampleOrder item)
    {
        TemplateSections.Add(CreateTemplateSection(
            "Scrap pickup and lugger",
            "Material-handler instructions for moving scrap to the correct lugger.",
            ("Part number", FieldValue(item, "Part number")),
            ("Pickup work center", FieldValue(item, "Pickup work center")),
            ("Quantity involved", FieldValue(item, "Quantity involved")),
            ("Scrap lugger", FieldValue(item, "Scrap lugger"))));

        TemplateSections.Add(CreateTemplateSection(
            "Material classification",
            "Confirm the material category before placement so scrap is not mixed into the wrong lugger.",
            ("Allowed categories", "3003 Aluminum; 5052 Aluminum; Galvanized Steel; Steel; Skeleton Frames; Other"),
            ("Scrap reason", FieldValue(item, "Scrap reason")),
            ("Classification approval", "Pending"),
            ("Safety requirements", "Not available")));

        AddRequestContextSection(item, "Scrap workflow", "Record handler pickup, lugger placement, confirmation, and any correction to the selected category.");
    }

    private void AddRequestContextSection(SampleOrder item, string title, string summary)
    {
        TemplateSections.Add(CreateTemplateSection(
            title,
            summary,
            ("Requested by", item.RequestedByName),
            ("Press or resource", item.RequestedPressName),
            ("Remaining time", item.RemainingTimeText),
            ("Handler status", "Pending assignment")));
    }

    private static string FieldValue(SampleOrder item, string label, string fallback = "Not available")
    {
        return item.Fields.FirstOrDefault(field => string.Equals(field.Label, label, StringComparison.OrdinalIgnoreCase))?.Value ?? fallback;
    }

    /// <summary>
    /// Resolves the underlying <see cref="WaitlistRequest"/> for a live request row (via its
    /// <see cref="SampleOrder.RequestId"/>), or null for static sample rows / when the service is absent.
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
    private static string WorkOrderText(SampleOrder item, WaitlistRequest? request)
    {
        var fieldValue = item.Fields
            .FirstOrDefault(field => string.Equals(field.Label, "Work order", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        if (!string.IsNullOrWhiteSpace(fieldValue) && !string.Equals(fieldValue, "Not available", StringComparison.OrdinalIgnoreCase))
        {
            return fieldValue!;
        }

        var jobId = request?.ActiveSetupJobId;
        return string.IsNullOrWhiteSpace(jobId) ? "Not available" : jobId!;
    }

    /// <summary>Requester employee number for the request, or 'Not available' when none is known.</summary>
    private static string EmployeeNumberText(WaitlistRequest? request, SampleOrder item)
    {
        var employeeNumber = request is not null && !string.IsNullOrWhiteSpace(request.RequesterEmployeeNumber)
            ? request.RequesterEmployeeNumber
            : item.RequesterEmployeeNumber;
        return string.IsNullOrWhiteSpace(employeeNumber) ? "Not available" : employeeNumber;
    }

    private static WaitlistDetailTemplateSection CreateTemplateSection(
        string title,
        string summary,
        params (string Label, string Value)[] fields)
    {
        var section = new WaitlistDetailTemplateSection
        {
            Title = title,
            Summary = summary
        };

        foreach (var field in fields)
        {
            section.Fields.Add(new WaitlistDetailTemplateField
            {
                Label = field.Label,
                Value = field.Value
            });
        }

        return section;
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
            var candidate = FieldValue(item, label).Trim();
            if (string.IsNullOrWhiteSpace(candidate) || string.Equals(candidate, "Not available", StringComparison.OrdinalIgnoreCase))
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
