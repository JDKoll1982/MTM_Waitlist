using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.ViewModels;

/// <summary>
/// The who-holds-this view: the roles whose baselines give a feature, and only the people who differ from their own
/// role's baseline (FR-075 to FR-079).
/// </summary>
/// <remarks>
/// <para>
/// <b>Read-only.</b> Nothing here writes anything, and choosing a differing person opens that person's own page,
/// because every change still happens there (FR-077).
/// </para>
/// <para>
/// <b>Nothing blocks the interface thread.</b> The two reads are asynchronous and the control binds to the result,
/// so the screen never freezes while the answer is fetched (FR-114).
/// </para>
/// <para>
/// <b>A feature nobody holds is stated in words</b> rather than shown as an empty region, and a holder who has been
/// switched off is listed and marked (FR-078, FR-079).
/// </para>
/// </remarks>
public partial class PermissionHoldersViewModel : ObservableObject
{
    /// <summary>The person's page, as the page service routes it: a differing person opens their own page.</summary>
    internal const string PersonPageViewModelName = "MTM_Waitlist.Module_Settings.ViewModels.EditUserViewModel";

    private readonly IPermissionAdministrationService _permissionAdministrationService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// True while the picker is being given its opening choice. That choice is the control setting itself up, not
    /// the reader choosing, so it must not start a read of its own: doing so would make the caller's own read a
    /// second one and read the store twice for one answer.
    /// </summary>
    private bool _isChoosingOpeningFeature;

    public PermissionHoldersViewModel(
        IPermissionAdministrationService permissionAdministrationService,
        INavigationService navigationService)
    {
        _permissionAdministrationService = permissionAdministrationService ?? throw new ArgumentNullException(nameof(permissionAdministrationService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <summary>Every declared permission, because every one of them appears on the page (FR-047).</summary>
    public ObservableCollection<PermissionFeatureOption> Features { get; } = new();

    [ObservableProperty]
    public partial PermissionFeatureOption? SelectedFeature
    {
        get; set;
    }

    /// <summary>The answer for the chosen feature, or <c>null</c> before one is chosen.</summary>
    [ObservableProperty]
    public partial PermissionHoldersView? View
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsBusy
    {
        get; set;
    }

    /// <summary>Whether the store could not be read. Only ever true together with a stated reason.</summary>
    [ObservableProperty]
    public partial bool IsStoreUnavailable
    {
        get; set;
    }

    /// <summary>What the view says about the last thing that happened, in the reader's words.</summary>
    [ObservableProperty]
    public partial string MessageText
    {
        get; set;
    } = string.Empty;

    public string HeadingText => "Permissions_Holders.Heading".GetLocalized();

    public string FeatureLabelText => "Permissions_Holders.Feature.Label".GetLocalized();

    public string RolesHeadingText => "Permissions_Holders.Roles.Heading".GetLocalized();

    public string PeopleHeadingText => "Permissions_Holders.People.Heading".GetLocalized();

    /// <summary>What a feature nobody holds is told with, rather than an empty region (FR-078).</summary>
    public string NobodyText => "Permissions_Holders.Nobody".GetLocalized();

    public string UnavailableText => "UserManagement_State.Unavailable".GetLocalized();

    public string RetryText => "UserManagement_State.Retry".GetLocalized();

    /// <summary>Whether nobody holds the chosen feature, which is stated in words (FR-078).</summary>
    public bool NobodyHoldsIt => View?.NobodyHoldsIt == true;

    /// <summary>Whether any role's baseline gives it.</summary>
    public bool HasRoles => View?.HasRoles == true;

    /// <summary>Whether anybody differs from their role for it.</summary>
    public bool HasPeople => View?.HasPeople == true;

    partial void OnViewChanged(PermissionHoldersView? value)
    {
        OnPropertyChanged(nameof(NobodyHoldsIt));
        OnPropertyChanged(nameof(HasRoles));
        OnPropertyChanged(nameof(HasPeople));
    }

    /// <summary>Fills the picker from the declaration, so every permission can be asked about (FR-047).</summary>
    public void LoadFeatures()
    {
        Features.Clear();

        foreach (var entry in PermissionRegistry.All)
        {
            Features.Add(new PermissionFeatureOption(entry.Key, PermissionRegistry.Label(entry.Key)));
        }

        if (SelectedFeature is not null)
        {
            return;
        }

        _isChoosingOpeningFeature = true;
        try
        {
            SelectedFeature = Features.FirstOrDefault();
        }
        finally
        {
            _isChoosingOpeningFeature = false;
        }
    }

    partial void OnSelectedFeatureChanged(PermissionFeatureOption? value)
    {
        if (_isChoosingOpeningFeature)
        {
            return;
        }

        _ = LoadAsync();
    }

    /// <summary>Reads the answer for the chosen feature. The reader's own retry goes through here.</summary>
    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var feature = SelectedFeature;
        if (feature is null)
        {
            View = null;
            return;
        }

        IsBusy = true;
        IsStoreUnavailable = false;
        MessageText = string.Empty;

        try
        {
            var holders = await _permissionAdministrationService
                .GetHoldersAsync(feature.Key, cancellationToken)
                .ConfigureAwait(true);

            View = new PermissionHoldersView(feature.Key, feature.LabelText, holders);
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error(
                "PermissionHolders",
                ex,
                "The holders could not be read, so the view shows its unavailable state rather than an empty answer.");

            View = null;
            IsStoreUnavailable = true;
            MessageText = UnavailableText;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Opens a differing person's own page, which is the only place a person is changed (FR-077).
    /// </summary>
    [RelayCommand]
    public void OpenPerson(PermissionHolderRow? person)
    {
        if (person is null)
        {
            return;
        }

        _navigationService.NavigateTo(PersonPageViewModelName, person.UserId);
    }
}
