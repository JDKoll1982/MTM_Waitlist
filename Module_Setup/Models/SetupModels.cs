using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Module_Setup.Models;

public enum SetupWorkflowStep
{
    WorkstationSelection,
    WorkOrderEntry,
    PartSelection,
    SequenceSelection,
    DunnageTypeSelection,
    DunnagePartSelection,
    Review
}

public sealed class SetupWorkflowState : ObservableObject
{
    private string _workOrderInput = string.Empty;
    private string _normalizedWorkOrder = string.Empty;
    private string _validationMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private string _selectedPartNumber = string.Empty;
    private string _selectedSequence = string.Empty;
    private string _selectedDunnageTypeId = string.Empty;
    private string _selectedDunnagePartId = string.Empty;
    private string _selectedWorkCenter = string.Empty;
    private string _selectedScrapType = string.Empty;
    private bool _requiresReplacementConfirmation;
    private bool _hasUnsavedChanges;
    private SetupWorkflowStep _currentStep = SetupWorkflowStep.WorkstationSelection;

    public ObservableCollection<SetupPartResult> PartResults { get; } = new();

    public ObservableCollection<SetupWorkstation> Workstations { get; } = new();

    public ObservableCollection<SetupSequenceResult> SequenceResults { get; } = new();

    public ObservableCollection<SetupSubordinatePart> SubordinateParts { get; } = new();

    public ObservableCollection<SetupDunnageType> DunnageTypes { get; } = new();

    public ObservableCollection<SetupDunnagePart> DunnageParts { get; } = new();

    public ObservableCollection<SetupDunnagePart> SelectedDunnageParts { get; } = new();

    public ObservableCollection<string> ScrapTypes { get; } = new();

    public string WorkOrderInput
    {
        get => _workOrderInput;
        set => SetProperty(ref _workOrderInput, value);
    }

    public string NormalizedWorkOrder
    {
        get => _normalizedWorkOrder;
        set => SetProperty(ref _normalizedWorkOrder, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedPartNumber
    {
        get => _selectedPartNumber;
        set => SetProperty(ref _selectedPartNumber, value);
    }

    public string SelectedSequence
    {
        get => _selectedSequence;
        set => SetProperty(ref _selectedSequence, value);
    }

    public string SelectedDunnageTypeId
    {
        get => _selectedDunnageTypeId;
        set => SetProperty(ref _selectedDunnageTypeId, value);
    }

    public string SelectedDunnagePartId
    {
        get => _selectedDunnagePartId;
        set => SetProperty(ref _selectedDunnagePartId, value);
    }

    public string SelectedWorkCenter
    {
        get => _selectedWorkCenter;
        set => SetProperty(ref _selectedWorkCenter, value);
    }

    public string SelectedScrapType
    {
        get => _selectedScrapType;
        set => SetProperty(ref _selectedScrapType, value);
    }

    public bool RequiresReplacementConfirmation
    {
        get => _requiresReplacementConfirmation;
        set => SetProperty(ref _requiresReplacementConfirmation, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public SetupWorkflowStep CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    public string SelectedDunnageSummary => SelectedDunnageParts.Count == 0
        ? "None selected"
        : string.Join(", ", SelectedDunnageParts.Select(part => part.DisplayName));

    public void UpdateSelectedDunnageSummary()
    {
        OnPropertyChanged(nameof(SelectedDunnageSummary));
    }

    public void Reset()
    {
        WorkOrderInput = string.Empty;
        NormalizedWorkOrder = string.Empty;
        ValidationMessage = string.Empty;
        StatusMessage = string.Empty;
        SelectedPartNumber = string.Empty;
        SelectedSequence = string.Empty;
        SelectedDunnageTypeId = string.Empty;
        SelectedDunnagePartId = string.Empty;
        SelectedWorkCenter = string.Empty;
        SelectedScrapType = string.Empty;
        RequiresReplacementConfirmation = false;
        HasUnsavedChanges = false;
        CurrentStep = SetupWorkflowStep.WorkstationSelection;

        Workstations.Clear();
        PartResults.Clear();
        SequenceResults.Clear();
        SubordinateParts.Clear();
        DunnageTypes.Clear();
        DunnageParts.Clear();
        SelectedDunnageParts.Clear();
        ScrapTypes.Clear();
        OnPropertyChanged(nameof(SelectedDunnageSummary));
    }
}

public sealed class SetupWorkstation
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string CurrentWorkOrder { get; set; } = string.Empty;

    public string CurrentPartNumber { get; set; } = string.Empty;

    public string CurrentSequenceNumber { get; set; } = string.Empty;

    public string CurrentJobDisplay => string.IsNullOrWhiteSpace(CurrentWorkOrder)
        ? "Current Job: None"
        : $"Current Job: {CurrentWorkOrder}/{CurrentSequenceNumber}";

    public string CurrentPartDisplay => string.IsNullOrWhiteSpace(CurrentPartNumber)
        ? "Part Number: None"
        : $"Part Number: {CurrentPartNumber}";
}

public sealed class SetupPartResult
{
    public string PartNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string WorkCenter { get; set; } = string.Empty;

    public string Summary => string.IsNullOrWhiteSpace(Description)
        ? PartNumber
        : $"{PartNumber} | {Description}";
}

public sealed class SetupSequenceResult
{
    public string SequenceNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}

public sealed class SetupSubordinatePart
{
    public string Category { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal OnHandQuantity { get; set; }

    public string User8 { get; set; } = string.Empty;

    public string SelectedScrapType { get; set; } = string.Empty;

    public bool IsLowStock { get; set; }

    public bool IsOutOfStock => OnHandQuantity <= 0;

    public string CategoryDisplayName => NormalizeCategory(Category);

    public string CategoryGlyph => Category switch
    {
        "Coil" => "\uEC14",
        "Die" => "\uE7B8",
        "Component" => "\uE8FD",
        "Flatstock" => "\uEECA",
        _ => "\uE9CE"
    };

    public string CategoryAccentKey => Category switch
    {
        "Coil" => "Coil",
        "Die" => "Die",
        "Component" => "Component",
        "Flatstock" => "Flatstock",
        _ => "Other"
    };

    public string StockMessage => IsOutOfStock
        ? "NONE ON HAND!"
        : IsLowStock
            ? "LOW STOCK"
            : "IN STOCK";

    public string StockStateKey => IsOutOfStock
        ? "OutOfStock"
        : IsLowStock
            ? "LowStock"
            : "InStock";

    public string OnHandDisplay => OnHandQuantity.ToString("0.##");

    public string LocationDisplay => string.IsNullOrWhiteSpace(Location) ? "Unassigned" : Location;

    private static string NormalizeCategory(string category)
    {
        return category switch
        {
            "Flatstock" => "Flat Stock",
            _ => string.IsNullOrWhiteSpace(category) ? "Other" : category
        };
    }

}

public sealed class SetupSubordinatePartGroup
{
    public string Category { get; set; } = string.Empty;

    public IReadOnlyList<SetupSubordinatePart> Parts { get; set; } = Array.Empty<SetupSubordinatePart>();

    public string PartCountLabel => Parts.Count == 1 ? "1 part" : $"{Parts.Count} parts";

    public string CategoryDisplayName => Parts.FirstOrDefault()?.CategoryDisplayName ?? (string.IsNullOrWhiteSpace(Category) ? "Other" : Category);

    public string CategoryGlyph => Parts.FirstOrDefault()?.CategoryGlyph ?? "\uE9CE";

    public string CategoryAccentKey => Parts.FirstOrDefault()?.CategoryAccentKey ?? "Other";
}

public sealed class SetupDunnageType
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string IconGlyph { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
}

public sealed class SetupDunnagePart
{
    private const string NoImagePath = "Assets/WindowIcon.ico";

    public string Id { get; set; } = string.Empty;

    public string TypeId { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;

    public bool IsSelectedForPair { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public string DisplayImagePath => HasImage ? ImagePath : NoImagePath;

    public string ImageFallbackText => HasImage ? string.Empty : "No image available";
}

public sealed class SetupSaveRequest
{
    public string WorkOrder { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public string SequenceNumber { get; set; } = string.Empty;

    public string WorkCenter { get; set; } = string.Empty;

    public string SelectedDunnageTypeId { get; set; } = string.Empty;

    public string SelectedDunnagePartId { get; set; } = string.Empty;

    public string SelectedScrapType { get; set; } = string.Empty;

    public IReadOnlyList<SetupSubordinatePart> SubordinateParts { get; set; } = Array.Empty<SetupSubordinatePart>();

    public IReadOnlyList<SetupDunnagePart> SelectedDunnageParts { get; set; } = Array.Empty<SetupDunnagePart>();
}

public sealed class SetupSaveResult
{
    public bool Success { get; set; }

    public bool RequiresReplacementConfirmation { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class SetupLookupResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<SetupPartResult> Parts { get; set; } = Array.Empty<SetupPartResult>();
}

public sealed class SetupSelectionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}