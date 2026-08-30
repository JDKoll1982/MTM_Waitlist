using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Contracts.ViewModels;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.ViewModels;

public partial class WaitlistViewDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly IBuildingSelectionService _buildingSelectionService;
    private readonly IImageLocationService? _imageLocationService;
    private IDisposable? _imageLocationSubscription;

    [ObservableProperty]
    public partial SampleOrder? Item
    {
        get; set;
    }

    public ObservableCollection<WaitlistDetailTemplateSection> TemplateSections { get; } = new();

    public WaitlistViewDetailViewModel(
        INavigationService navigationService,
        ISampleDataService sampleDataService,
        IBuildingSelectionService buildingSelectionService,
        IImageLocationService? imageLocationService = null)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(sampleDataService);
        ArgumentNullException.ThrowIfNull(buildingSelectionService);

        _navigationService = navigationService;
        _sampleDataService = sampleDataService;
        _buildingSelectionService = buildingSelectionService;
        _imageLocationService = imageLocationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        var orderId = parameter switch
        {
            int intId => intId,
            long longId when longId <= int.MaxValue && longId >= int.MinValue => (int)longId,
            _ => (int?)null
        };

        if (orderId.HasValue)
        {
            var data = _sampleDataService.GetSampleOrders(_buildingSelectionService.SelectedBuilding);
            Item = data.OfType<SampleOrder>().FirstOrDefault(i => i.Id == orderId.Value);
        }

        if (_imageLocationSubscription is null
            && _imageLocationService is not null
            && _imageLocationService.IsInitialized)
        {
            _imageLocationSubscription = _imageLocationService.SubscribeToImageLocationChanges(OnImageLocationChanged);
        }

        LoadTemplateSections();
    }

    public void OnNavigatedFrom()
    {
        _imageLocationSubscription?.Dispose();
        _imageLocationSubscription = null;
    }

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
            ("Work order", "Not available"),
            ("Work center", FieldValue(item, "Requesting work center", item.RequestedPressName)),
            ("Requesting user", item.RequestedByName),
            ("Employee number", "Not available")));

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
