using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;

namespace MTM_Waitlist.GenericRequest.ViewModels;

/// <summary>The scrap type step: pick the scrap type the request needs.</summary>
/// <remarks>
/// <para>
/// The choices are the plant's own list (FR-002), so a type the plant does not carry cannot be picked. When the
/// requester's type is missing, a person entitled to maintain the lugger arrangement can add it here through the same
/// assignment the arrangement editor writes (FR-017): the type is given to a lugger that person names, and every later
/// scrap request is offered it because it is now part of the arrangement.
/// </para>
/// <para>
/// The add is gated on <see cref="PermissionKeys.SettingsLuggerArrangement"/>, because assigning a type decides where
/// every scrap request for that type dumps — the work-order cards included (FR-018).
/// </para>
/// </remarks>
public sealed partial class GenericRequestScrapTypeViewModel : GenericRequestStepViewModel
{
    private const string LogCategory = "WaitlistGenericRequest";

    private const string BlankTypeKey = "GenericRequest_Scrap_Add_Blank.Text";
    private const string NoLuggerKey = "GenericRequest_Scrap_Add_NoLugger.Text";
    private const string FailedKey = "GenericRequest_Scrap_Add_Failed.Text";

    private readonly IGenericRequestStepData _data;
    private readonly ILuggerArrangementService _arrangementService;
    private readonly IPermissionService _permissionService;
    private readonly IPersonIdentity _personIdentity;

    public GenericRequestScrapTypeViewModel(
        IGenericRequestStepData data,
        IGenericRequestNavigator navigator,
        ILuggerArrangementService arrangementService,
        IPermissionService permissionService,
        IPersonIdentity personIdentity)
        : base(navigator)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(arrangementService);
        ArgumentNullException.ThrowIfNull(permissionService);
        ArgumentNullException.ThrowIfNull(personIdentity);

        _data = data;
        _arrangementService = arrangementService;
        _permissionService = permissionService;
        _personIdentity = personIdentity;
    }

    public override GenericRequestStep Step => GenericRequestStep.Scrap;

    public override string TitleKey => "GenericRequest_Scrap_Title.Text";

    public override string IntroKey => "GenericRequest_Scrap_Intro.Text";

    public override string EmptyKey => "GenericRequest_Scrap_Empty.Text";

    public ObservableCollection<GenericRequestScrapOption> Options { get; } = new();

    [ObservableProperty]
    public partial GenericRequestScrapOption? Selected { get; set; }

    /// <summary>Whether the add affordance is drawn at all: only a person who maintains the arrangement sees it.</summary>
    [ObservableProperty]
    public partial bool IsAddAvailable { get; set; }

    /// <summary>Whether the add panel is open.</summary>
    [ObservableProperty]
    public partial bool IsAdding { get; set; }

    /// <summary>What the person typed, before it is trimmed and de-duplicated.</summary>
    [ObservableProperty]
    public partial string NewScrapType { get; set; } = string.Empty;

    /// <summary>Every lugger in the arrangement's own order, so the person names where the type dumps.</summary>
    public ObservableCollection<Lugger> Luggers { get; } = new();

    [ObservableProperty]
    public partial Lugger? SelectedLugger { get; set; }

    /// <summary>What the add panel says when it refuses or fails, or empty when it has nothing to report.</summary>
    [ObservableProperty]
    public partial string AddMessage { get; set; } = string.Empty;

    /// <summary>Whether a write is in flight, so the add cannot be run twice over.</summary>
    [ObservableProperty]
    public partial bool IsAddingBusy { get; set; }

    public bool HasAddMessage => AddMessage.Length > 0;

    /// <summary>Whether the add affordance is drawn: only a person who maintains the arrangement sees it (FR-018).</summary>
    public Visibility AddVisibility => IsAddAvailable ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether the add panel is drawn.</summary>
    public Visibility AddPanelVisibility => IsAdding ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Whether the add panel's own report is drawn.</summary>
    public Visibility AddMessageVisibility => HasAddMessage ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>The step is answerable once a type is chosen; nothing chosen means nothing may advance (FR-003).</summary>
    protected override bool CanAnswer => Selected is not null;

    partial void OnSelectedChanged(GenericRequestScrapOption? value) => NotifyAnswerChanged();

    partial void OnAddMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasAddMessage));
        OnPropertyChanged(nameof(AddMessageVisibility));
    }

    partial void OnIsAddAvailableChanged(bool value) => OnPropertyChanged(nameof(AddVisibility));

    partial void OnIsAddingChanged(bool value) => OnPropertyChanged(nameof(AddPanelVisibility));

    /// <summary>Opens the add panel and fills the lugger list, reading it once when the panel opens (FR-017).</summary>
    [RelayCommand]
    public async Task ShowAddAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAddAvailable)
        {
            return;
        }

        AddMessage = string.Empty;
        IsAdding = true;

        await LoadLuggersAsync(cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Closes the add panel and forgets what was typed, so a re-open starts empty.</summary>
    [RelayCommand]
    public void CancelAdd()
    {
        IsAdding = false;
        NewScrapType = string.Empty;
        SelectedLugger = null;
        AddMessage = string.Empty;
    }

    /// <summary>
    /// Gives the typed scrap type to the chosen lugger, then re-reads the list and selects it (FR-017). A type the
    /// list already carries is selected rather than written, so the arrangement gains no duplicate.
    /// </summary>
    [RelayCommand]
    public async Task AddScrapTypeAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAddAvailable || IsAddingBusy)
        {
            return;
        }

        var scrapType = (NewScrapType ?? string.Empty).Trim();
        if (scrapType.Length == 0)
        {
            AddMessage = BlankTypeKey.GetLocalized();
            return;
        }

        var alreadyOffered = Options.FirstOrDefault(option =>
            string.Equals(option.ScrapType.Trim(), scrapType, StringComparison.OrdinalIgnoreCase));

        if (alreadyOffered is not null)
        {
            Selected = alreadyOffered;
            CancelAdd();
            return;
        }

        if (SelectedLugger is not { } lugger)
        {
            AddMessage = NoLuggerKey.GetLocalized();
            return;
        }

        IsAddingBusy = true;

        try
        {
            var snapshot = await _arrangementService.GetSnapshotAsync(cancellationToken).ConfigureAwait(true);
            var key = Normalise(scrapType);
            var position = snapshot.Rows
                .Count(row => string.Equals(row.ScrapTypeKey, key, StringComparison.Ordinal)) + 1;

            await _arrangementService
                .AssignScrapTypeAsync(
                    scrapType,
                    lugger.PublicId,
                    position,
                    _personIdentity.EmployeeNumber ?? string.Empty,
                    _personIdentity.DisplayName ?? string.Empty,
                    cancellationToken)
                .ConfigureAwait(true);

            CancelAdd();

            // The list is re-read rather than grown locally: the arrangement is the source, and what it now holds is
            // what the next request is offered.
            await LoadAsync().ConfigureAwait(true);
            Selected = Options.FirstOrDefault(option =>
                string.Equals(option.ScrapType.Trim(), scrapType, StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LuggerArrangementRefusalException refusal)
        {
            AddMessage = refusal.Message;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogCategory, exception, $"Adding the scrap type '{scrapType}' failed.");
            AddMessage = FailedKey.GetLocalized();
        }
        finally
        {
            IsAddingBusy = false;
        }
    }

    protected override async Task LoadCoreAsync()
    {
        var result = await _data.ReadScrapTypesAsync();

        Options.Clear();
        foreach (var option in result.Items)
        {
            Options.Add(option);
        }

        Selected = Options.FirstOrDefault(option =>
            string.Equals(option.ScrapType.Trim(), Draft.ScrapType.Trim(), StringComparison.OrdinalIgnoreCase));

        IsAddAvailable = await _permissionService
            .HasPermissionAsync(PermissionKeys.SettingsLuggerArrangement)
            .ConfigureAwait(true);

        Apply(result);
    }

    protected override void CommitToDraft()
    {
        if (Selected is { } selected)
        {
            Draft.ScrapType = selected.ScrapType;
        }
    }

    private static string Normalise(string? scrapType) => scrapType?.Trim().ToLowerInvariant() ?? string.Empty;

    private async Task LoadLuggersAsync(CancellationToken cancellationToken)
    {
        Luggers.Clear();

        try
        {
            foreach (var lugger in await _arrangementService.GetLuggersAsync(cancellationToken).ConfigureAwait(true))
            {
                Luggers.Add(lugger);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogCategory, exception, "Reading the luggers to add a scrap type failed.");
            AddMessage = FailedKey.GetLocalized();
            return;
        }

        SelectedLugger = Luggers.FirstOrDefault(lugger => lugger.CanReceiveMaterial) ?? Luggers.FirstOrDefault();
    }
}
