using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_DevTools.Models;
using MTM_Waitlist.Module_DevTools.Services;

namespace MTM_Waitlist.Module_DevTools.ViewModels;

public partial class RequestTypeBuilderViewModel : ObservableRecipient
{
    private readonly IDevToolsRequestTypeService _requestTypeService;

    [ObservableProperty]
    public partial string RequestTypeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CardFieldNameInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RequestFieldDataTypeOption? SelectedCardDataType { get; set; }

    [ObservableProperty]
    public partial string DetailFieldNameInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RequestFieldDataTypeOption? SelectedDetailDataType { get; set; }

    [ObservableProperty]
    public partial RequestTypeFieldDefinition? SelectedCardField { get; set; }

    [ObservableProperty]
    public partial RequestTypeFieldDefinition? SelectedDetailField { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Define a request type and save it to mtm_waitlist.";

    [ObservableProperty]
    public partial string? SelectedPresetPrefix { get; set; }

    [ObservableProperty]
    public partial string? SelectedPresetSuffix { get; set; }

    public ObservableCollection<RequestTypeFieldDefinition> CardFields { get; } = new();

    public ObservableCollection<RequestTypeFieldDefinition> DetailFields { get; } = new();

    public ObservableCollection<string> PresetPrefixes { get; } = new()
    {
        "Pickup",
        "Need",
        "Replace",
        "Restock",
        "Move",
        "Pull",
        "Deliver",
        "Stage",
        "Swap",
        "Return",
        "Expedite",
        "Correct"
    };

    public ObservableCollection<string> PresetSuffixes { get; } = new()
    {
        "Coils",
        "Flatstock",
        "Finished Goods",
        "NCM",
        "Material",
        "Press Line",
        "Coil Rack",
        "Staging",
        "Pallets",
        "Return Cart",
        "Tool Room",
        "WIP"
    };

    public IReadOnlyList<RequestFieldDataTypeOption> DataTypeOptions { get; } = BuildDataTypeOptions();

    public Func<Task<string?>>? PickImageFileAsync { get; set; }

    public RequestTypeBuilderViewModel(IDevToolsRequestTypeService requestTypeService)
    {
        ArgumentNullException.ThrowIfNull(requestTypeService);
        _requestTypeService = requestTypeService;

        SelectedCardDataType = DataTypeOptions[0];
        SelectedDetailDataType = DataTypeOptions[0];
    }

    [RelayCommand]
    private void ApplySelectedPresetPrefix()
    {
        if (string.IsNullOrWhiteSpace(SelectedPresetPrefix))
        {
            StatusText = "Choose a prefix suggestion first.";
            return;
        }

        RequestTypeName = MergeNamePart(RequestTypeName, SelectedPresetPrefix!, prepend: true);
        StatusText = "Prefix added to the request type name.";
    }

    [RelayCommand]
    private void ApplySelectedPresetSuffix()
    {
        if (string.IsNullOrWhiteSpace(SelectedPresetSuffix))
        {
            StatusText = "Choose a suffix suggestion first.";
            return;
        }

        RequestTypeName = MergeNamePart(RequestTypeName, SelectedPresetSuffix!, prepend: false);
        StatusText = "Suffix added to the request type name.";
    }

    [RelayCommand]
    private void ApplySelectedPresetPair()
    {
        if (string.IsNullOrWhiteSpace(SelectedPresetPrefix) || string.IsNullOrWhiteSpace(SelectedPresetSuffix))
        {
            StatusText = "Choose both a prefix and suffix first.";
            return;
        }

        var combined = $"{SelectedPresetPrefix!.Trim()} {SelectedPresetSuffix!.Trim()}";
        RequestTypeName = MergeNamePart(RequestTypeName, combined, prepend: false);
        StatusText = "Prefix and suffix added to the request type name.";
    }

    [RelayCommand]
    private async Task BrowseImageAsync()
    {
        if (PickImageFileAsync is null)
        {
            StatusText = "Image browsing is currently unavailable.";
            return;
        }

        var selectedPath = await PickImageFileAsync();
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            ImageFilePath = selectedPath.Trim();
        }
    }

    [RelayCommand]
    private void AddCardField()
    {
        if (CardFields.Count >= 5)
        {
            StatusText = "Card data is limited to 5 fields.";
            return;
        }

        var normalizedName = NormalizeFieldName(CardFieldNameInput);
        if (string.IsNullOrWhiteSpace(normalizedName) || SelectedCardDataType is null)
        {
            StatusText = "Enter a card field name and select a data type.";
            return;
        }

        if (CardFields.Any(field => string.Equals(field.FieldName, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "Card field names must be unique.";
            return;
        }

        CardFields.Add(new RequestTypeFieldDefinition(normalizedName, SelectedCardDataType.Value));
        CardFieldNameInput = string.Empty;
        StatusText = "Card field added.";
    }

    [RelayCommand]
    private void AddDetailField()
    {
        var normalizedName = NormalizeFieldName(DetailFieldNameInput);
        if (string.IsNullOrWhiteSpace(normalizedName) || SelectedDetailDataType is null)
        {
            StatusText = "Enter a detail field name and select a data type.";
            return;
        }

        if (DetailFields.Any(field => string.Equals(field.FieldName, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "Detail field names must be unique.";
            return;
        }

        DetailFields.Add(new RequestTypeFieldDefinition(normalizedName, SelectedDetailDataType.Value));
        DetailFieldNameInput = string.Empty;
        StatusText = "Detail field added.";
    }

    [RelayCommand]
    private void RemoveSelectedCardField()
    {
        if (SelectedCardField is null)
        {
            return;
        }

        CardFields.Remove(SelectedCardField);
        StatusText = "Card field removed.";
    }

    [RelayCommand]
    private void RemoveSelectedDetailField()
    {
        if (SelectedDetailField is null)
        {
            return;
        }

        DetailFields.Remove(SelectedDetailField);
        StatusText = "Detail field removed.";
    }

    [RelayCommand]
    private async Task SaveRequestTypeAsync()
    {
        var normalizedName = RequestTypeName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            StatusText = "Request type name is required.";
            return;
        }

        if (CardFields.Count == 0)
        {
            StatusText = "Add at least one card field.";
            return;
        }

        IsBusy = true;
        StatusText = "Saving request type...";

        try
        {
            var definition = new RequestTypeDefinition
            {
                RequestTypeName = normalizedName,
                ImageFilePath = string.IsNullOrWhiteSpace(ImageFilePath) ? null : ImageFilePath.Trim(),
                CardFields = CardFields.ToList(),
                DetailFields = DetailFields.ToList()
            };

            await _requestTypeService.SaveRequestTypeAsync(definition, Environment.UserName);

            RequestTypeName = string.Empty;
            ImageFilePath = string.Empty;
            CardFieldNameInput = string.Empty;
            DetailFieldNameInput = string.Empty;
            CardFields.Clear();
            DetailFields.Clear();
            StatusText = "Request type saved to mtm_waitlist.";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string NormalizeFieldName(string fieldName)
    {
        return fieldName.Trim();
    }

    private static string MergeNamePart(string currentName, string addition, bool prepend)
    {
        var trimmedCurrentName = currentName.Trim();
        var trimmedAddition = addition.Trim();

        if (string.IsNullOrWhiteSpace(trimmedCurrentName))
        {
            return trimmedAddition;
        }

        return prepend
            ? $"{trimmedAddition} {trimmedCurrentName}"
            : $"{trimmedCurrentName} {trimmedAddition}";
    }

    private static IReadOnlyList<RequestFieldDataTypeOption> BuildDataTypeOptions()
    {
        return Enum
            .GetValues<RequestFieldDataType>()
            .Select(dataType => new RequestFieldDataTypeOption
            {
                Value = dataType,
                Label = dataType.ToDisplayLabel()
            })
            .ToList();
    }
}
