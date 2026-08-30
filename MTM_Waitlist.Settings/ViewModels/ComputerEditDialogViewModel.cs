using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// Add / Edit computer dialog backing (Ref: 9). For a new computer the caller
/// supplies an optional detected hostname; for an existing computer it receives
/// the <see cref="ComputerRecord"/> to edit. Saves via
/// <see cref="IComputerRegistryService.UpsertComputerAsync"/> (new) or
/// <see cref="IComputerRegistryService.UpdateComputerAsync"/> (edit).
/// </summary>
public sealed partial class ComputerEditDialogViewModel : ObservableRecipient
{
    private readonly IComputerRegistryService _computerRegistryService;

    public ComputerEditDialogViewModel(IComputerRegistryService computerRegistryService)
    {
        _computerRegistryService = computerRegistryService;
    }

    public long? EditingId { get; private set; }

    public bool IsEditMode => EditingId.HasValue;

    [ObservableProperty]
    public partial string Title
    {
        get; set;
    } = "Add Computer";

    [ObservableProperty]
    public partial string ComputerName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string HostnameNormalized
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string MacAddressNormalized
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial string Description
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool IsRegistered
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial string ValidationError
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial bool CanSave
    {
        get; set;
    } = true;

    /// <summary>Initializes the dialog for adding a new computer.</summary>
    public void ConfigureForAdd(string? detectedComputerName = null, string? detectedMacAddress = null)
    {
        EditingId = null;
        Title = "Add Computer";
        ComputerName = detectedComputerName?.Trim() ?? string.Empty;
        HostnameNormalized = detectedComputerName?.Trim() ?? string.Empty;
        MacAddressNormalized = detectedMacAddress?.Trim() ?? string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        IsRegistered = true;
        ValidationError = string.Empty;
    }

    /// <summary>Initializes the dialog for editing an existing computer.</summary>
    public void ConfigureForEdit(ComputerRecord record)
    {
        EditingId = record.Id;
        Title = "Edit Computer";
        ComputerName = record.ComputerName;
        HostnameNormalized = record.ComputerName;
        MacAddressNormalized = record.MacAddressNormalized;
        DisplayName = record.DisplayName;
        Description = record.Description;
        IsRegistered = record.IsRegistered;
        ValidationError = string.Empty;
    }

    [RelayCommand]
    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ValidationError = "Display name is required.";
            CanSave = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ComputerName))
        {
            ValidationError = "Computer name is required.";
            CanSave = false;
            return;
        }

        ValidationError = string.Empty;
        CanSave = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Validate();
        if (!CanSave)
        {
            return;
        }

        try
        {
            if (IsEditMode && EditingId.HasValue)
            {
                await _computerRegistryService.UpdateComputerAsync(
                    EditingId.Value,
                    ComputerName,
                    HostnameNormalized,
                    MacAddressNormalized,
                    DisplayName,
                    Description,
                    IsRegistered).ConfigureAwait(true);
            }
            else
            {
                await _computerRegistryService.UpsertComputerAsync(
                    ComputerName,
                    HostnameNormalized,
                    MacAddressNormalized,
                    DisplayName,
                    Description).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            ValidationError = ex.Message.Contains("uq_core_computers_registry_display_name", StringComparison.OrdinalIgnoreCase)
                ? "That display name is already in use. Choose a different one."
                : $"Unable to save this computer: {ex.Message}";
        }
    }
}
