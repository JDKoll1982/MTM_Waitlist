using System.Reflection;

using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;

namespace MTM_Waitlist.Tests.Module_Settings;

[TestClass]
public sealed class SettingsViewModelIgnoredLocationsTests
{
    [TestMethod]
    public void DefaultSeed_PopulatesTheDefaultIgnoredLocations()
    {
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsIgnoredLocations]);

        CollectionAssert.AreEquivalent(
            new[] { "WC", "NCM", "V-WC", "NCM-VITS", "SHIP" },
            viewModel.IgnoredLocations.ToArray());
    }

    [TestMethod]
    public async Task AddAndRemove_PersistToLocalSettings()
    {
        var settings = new RecordingLocalSettingsService();
        var viewModel = BuildViewModel(settings, [PermissionKeys.SettingsIgnoredLocations]);

        // Add (auto-uppercased, dedupes) then remove.
        viewModel.IgnoredLocationInput = "wc";
        viewModel.AddIgnoredLocationCommand.Execute(null);
        await Task.Delay(30);
        Assert.IsTrue(viewModel.IgnoredLocations.Contains("WC"), "Lowercase entry should be added as uppercase.");

        viewModel.RemoveIgnoredLocationCommand.Execute("WC");
        await Task.Delay(30);
        Assert.IsFalse(viewModel.IgnoredLocations.Contains("WC"), "Removed location should be gone.");

        var stored = settings.ReadSettingAsync<List<string>?>("Feature.IgnoredLocations").GetAwaiter().GetResult();
        Assert.IsNotNull(stored);
        Assert.IsFalse(stored!.Contains("WC", StringComparer.OrdinalIgnoreCase));
        Assert.IsTrue(stored.Contains("NCM", StringComparer.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void PersistenceRoundTrip_SeedsFromStoredInsteadOfDefaults()
    {
        var settings = new RecordingLocalSettingsService();
        settings.SaveSettingAsync("Feature.IgnoredLocations", new List<string> { "WC", "SCRAP-1" }).GetAwaiter().GetResult();

        var viewModel = BuildViewModel(settings, [PermissionKeys.SettingsIgnoredLocations]);

        CollectionAssert.AreEquivalent(new[] { "WC", "SCRAP-1" }, viewModel.IgnoredLocations.ToArray());
    }

    [TestMethod]
    public void AddRejectsInvalidCode_WhenThePermissionIsNotHeld()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());

        // The gate is permission.settings.ignored_locations, and this person does not hold it.
        Assert.IsFalse(viewModel.CanManageIgnoredLocations);

        viewModel.IgnoredLocationInput = "SCRAP-2";
        viewModel.AddIgnoredLocationCommand.Execute(null);
        Assert.IsFalse(viewModel.IgnoredLocations.Contains("SCRAP-2"));
    }

    [TestMethod]
    public void CanManageIgnoredLocations_FollowsThePermissionAndNothingElse()
    {
        Assert.IsFalse(
            BuildViewModel(new RecordingLocalSettingsService()).CanManageIgnoredLocations,
            "A person who does not hold the permission may not manage ignored locations, whatever role they are on.");

        Assert.IsTrue(
            BuildViewModel(new RecordingLocalSettingsService(), [PermissionKeys.SettingsIgnoredLocations])
                .CanManageIgnoredLocations,
            "The shipped baseline for the permission is the whole answer.");

        Assert.IsFalse(
            BuildViewModel(new RecordingLocalSettingsService(), [PermissionKeys.SettingsPartPictures])
                .CanManageIgnoredLocations,
            "Holding a different permission opens nothing here: the gates are named and independent.");
    }

    [TestMethod]
    public async Task OnNavigatedTo_ReadsTheScreensGates_AndTheyStayShutUntilTheAnswerArrives()
    {
        var viewModel = BuildViewModelWithoutLoading(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsIgnoredLocations]);

        Assert.IsFalse(
            viewModel.CanManageIgnoredLocations,
            "No gate may answer before its read has returned, or the screen would offer a control it may have to take back.");
        Assert.IsFalse(viewModel.IsAdministrationCategoryVisible, "The Administration category starts hidden.");

        viewModel.OnNavigatedTo(null!);
        await viewModel.PermissionLoad;

        Assert.IsTrue(viewModel.CanManageIgnoredLocations, "The page's own load settles the gates.");
        Assert.IsFalse(
            viewModel.IsAdministrationCategoryVisible,
            "A reader entitled to neither Administration entry does not see the category at all (FR-081).");
    }

    [TestMethod]
    public void LocationCodeValidation_AcceptsCodePatternAndRejectsInvalid()
    {
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("WC"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("NCM"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("V-WC"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("NCM-VITS"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("SHIP"));
        Assert.IsTrue(SettingsViewModel.IsValidLocationCode("A1-B2"));

        Assert.IsFalse(SettingsViewModel.IsValidLocationCode(string.Empty));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("-WC"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("WC-"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("A B"));
        Assert.IsFalse(SettingsViewModel.IsValidLocationCode("A@B"));
    }

    [TestMethod]
    public void NewRequestAlerts_DefaultsOff()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());
        Assert.IsFalse(viewModel.NewRequestAlertsEnabled);
    }

    [TestMethod]
    public void NewRequestAlerts_PanelAndCategoryMatchSearch_AndTheChangeIsAnnounced()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());
        Assert.IsTrue(viewModel.IsNewRequestAlertsPanelVisible, "Panel visible with no search query.");

        var announced = new List<string>();
        viewModel.PropertyChanged += (_, args) => announced.Add(args.PropertyName ?? string.Empty);

        viewModel.SearchQuery = "notification";

        Assert.IsTrue(viewModel.IsNewRequestAlertsPanelVisible, "Panel should match 'notification'.");
        Assert.IsTrue(viewModel.IsOperationsCategoryVisible, "Operations category should include the alerts panel.");

        // A correct value is not enough. The panel only follows the search term if the change is announced,
        // and the previous form of this check read the property directly so it passed either way (FR-021).
        CollectionAssert.Contains(
            announced,
            nameof(SettingsViewModel.IsNewRequestAlertsPanelVisible),
            "The panel must announce a change when the search term changes.");
    }

    [TestMethod]
    public async Task NewRequestAlerts_TogglePersistsOnlyWhenTheInstallationCanDeliver()
    {
        var settings = new RecordingLocalSettingsService();
        var viewModel = BuildViewModel(settings);

        viewModel.NewRequestAlertsEnabled = true;
        await Task.Delay(30);

        var stored = settings.ReadSettingAsync<bool?>(NewRequestAlertService.SettingKeyName).GetAwaiter().GetResult();

        if (viewModel.IsNewRequestAlertsAvailable)
        {
            Assert.IsTrue(stored == true, "An installation that can deliver must persist the preference under the alert key.");
        }
        else
        {
            Assert.IsNull(
                stored,
                "An installation that cannot deliver must not record a preference it cannot honour.");
        }
    }

    [TestMethod]
    public void CanRequestCacheRefresh_AnswersFromTheSettingsPermissionOnly()
    {
        // The gate is permission.settings.cache_refresh, and its shipped baseline is the same trio the Max
        // Allotted Time panel gates on. The subse rule that ties it to the service host's own permission is
        // asserted against the shipped baselines, where both sides' data is visible (FR-057).
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService()).CanRequestCacheRefresh);
        Assert.IsFalse(
            BuildViewModel(new RecordingLocalSettingsService(), [PermissionKeys.SettingsHotWorkCenters])
                .CanRequestCacheRefresh,
            "A different permission opens nothing here.");

        var holding = BuildViewModel(new RecordingLocalSettingsService(), [PermissionKeys.SettingsCacheRefresh]);
        Assert.IsTrue(holding.CanRequestCacheRefresh);

        // The panel is "access only": without the permission the surface is not drawn at all.
        Assert.IsFalse(BuildViewModel(new RecordingLocalSettingsService()).IsCacheRefreshPanelVisible);
        Assert.IsTrue(holding.IsCacheRefreshPanelVisible);
    }

    [TestMethod]
    public async Task RequestCacheRefresh_DoesNotReachTheService_WithoutThePermission()
    {
        var client = new FakeMockServiceRefreshClient();
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), refreshClient: client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual(0, client.CallCount, "A person outside the gate must never reach the service.");
        Assert.AreEqual(string.Empty, viewModel.CacheRefreshStatusMessage);
    }

    [TestMethod]
    public async Task RequestCacheRefresh_ReportsTheShapesThatRefreshed()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = new RefreshRequestResult
            {
                Succeeded = true,
                ShapeOutcomes = new Dictionary<string, string>
                {
                    ["work_order_lookup"] = "refreshed",
                    ["inventory_locations"] = "refreshed",
                },
            },
        };
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsCacheRefresh],
            client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual(1, client.CallCount);
        Assert.IsNull(client.LastShapeKeys, "The panel asks for every enabled shape, not a subset.");
        StringAssert.Contains(viewModel.CacheRefreshStatusMessage, "2 read shape(s) refreshed");
        Assert.IsFalse(viewModel.IsCacheRefreshing, "The busy indicator must clear when the request finishes.");
    }

    [TestMethod]
    public async Task RequestCacheRefresh_NamesAShapeThatCouldNotRefresh()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = new RefreshRequestResult
            {
                Succeeded = true,
                ShapeOutcomes = new Dictionary<string, string>
                {
                    ["work_order_lookup"] = "refreshed",
                    ["inventory_locations"] = "skippedSourceUnreachable",
                },
            },
        };
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsCacheRefresh],
            client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        StringAssert.Contains(viewModel.CacheRefreshStatusMessage, "inventory_locations: skippedSourceUnreachable");
    }

    [TestMethod]
    public async Task RequestCacheRefresh_ReportsAnUnconfiguredOrAbsentService_WithoutThrowing()
    {
        var client = new FakeMockServiceRefreshClient
        {
            Result = RefreshRequestResult.Unavailable("No endpoint is installed."),
        };
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsCacheRefresh],
            client);

        await viewModel.RequestCacheRefreshCommand.ExecuteAsync(null);

        Assert.AreEqual("No endpoint is installed.", viewModel.CacheRefreshStatusMessage);
        Assert.IsFalse(viewModel.IsCacheRefreshing);
    }

    [TestMethod]
    public void NewRequestAlerts_ReportsTheInstallationCapability()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());

        // The test host runs unpackaged, and the delivery path refuses to show a notification without
        // package identity (AppNotificationService.Initialize and Show both gate on RuntimeHelper.IsMSIX),
        // so this installation cannot deliver one and the panel must say so.
        Assert.IsFalse(
            MTM_Waitlist.Module_Core.Helpers.RuntimeHelper.IsMSIX,
            "This check assumes an unpackaged test host; the capability answer below is derived from it.");

        Assert.IsFalse(
            viewModel.IsNewRequestAlertsAvailable,
            "The panel must not offer a capability the delivery path would silently ignore.");

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.NewRequestAlertsUnavailableMessage),
            "The panel must explain why the control is unavailable.");
    }

    [TestMethod]
    public async Task NewRequestAlerts_DoesNotPersistWhileTheInstallationCannotDeliver()
    {
        var settings = new RecordingLocalSettingsService();
        var viewModel = BuildViewModel(settings);

        Assert.IsFalse(viewModel.IsNewRequestAlertsAvailable, "This check assumes an installation that cannot deliver.");

        viewModel.NewRequestAlertsEnabled = true;
        await Task.Delay(30);

        var stored = settings.ReadSettingAsync<bool?>(NewRequestAlertService.SettingKeyName).GetAwaiter().GetResult();

        Assert.IsNull(stored, "No preference may be written while the installation cannot deliver a notification.");
    }

    [TestMethod]
    public void VersionDescription_IsNotEmptyOnAnUnpackagedBuild()
    {
        // The About card's version value must say something on this build too, not only when the
        // packaged-only API is available (contract C4).
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());

        Assert.IsFalse(
            string.IsNullOrWhiteSpace(viewModel.VersionDescription),
            "The About card's version value must not be empty on the unpackaged build.");
    }

    [TestMethod]
    public void SearchQuery_ChangeAnnouncesEveryPanelThatConsumesTheTerm()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService());

        var announced = new List<string>();
        viewModel.PropertyChanged += (_, args) => announced.Add(args.PropertyName ?? string.Empty);

        viewModel.SearchQuery = "urgency";

        string[] previouslyMissing =
        [
            nameof(SettingsViewModel.IsNewRequestAlertsPanelVisible),
            nameof(SettingsViewModel.IsUrgencyAllotmentsPanelVisible),
            nameof(SettingsViewModel.IsImageLocationSettingsPanelVisible),
        ];

        foreach (var name in previouslyMissing)
        {
            CollectionAssert.Contains(
                announced,
                name,
                $"{name} must announce a change when the search term changes, or it keeps showing the previous term's answer.");
        }
    }

    [TestMethod]
    public void SearchRegistration_CoversEveryPropertyThatConsumesTheTerm()
    {
        var declared = DeclaredSearchAwareProperties();

        Assert.IsTrue(
            declared.Count >= 10,
            $"Only {declared.Count} search-aware properties are declared; the list must name every property that consumes the term.");

        var method = typeof(SettingsViewModel).GetMethod("MatchesSearch", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "The search predicate moved; update this check rather than deleting it.");

        var marker = BitConverter.GetBytes(method.MetadataToken);

        var consumers = typeof(SettingsViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => GetterMentions(property, marker))
            .Select(property => property.Name)
            .ToList();

        Assert.IsTrue(
            consumers.Count > 0,
            "No property was found to consume the search term, so a clean result would prove nothing.");

        var unregistered = consumers.Where(name => !declared.Contains(name, StringComparer.Ordinal)).ToList();

        Assert.AreEqual(
            0,
            unregistered.Count,
            "These properties filter on the search term but are not registered, so they never re-evaluate when it changes: "
                + string.Join(", ", unregistered));
    }

    [TestMethod]
    public void TheMinutesPanel_AndThePicturePanel_EachKeepTheirOwnGate()
    {
        var minutesOnly = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsUrgencyMinutes]);
        var picturesOnly = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsPartPictures]);
        var neither = BuildViewModel(new RecordingLocalSettingsService());

        // The minutes editor is governed by permission.settings.urgency_minutes and nothing else...
        Assert.IsTrue(minutesOnly.UrgencyAllotments.CanManageUrgencySettings);
        Assert.IsTrue(minutesOnly.IsUrgencyAllotmentsPanelVisible);
        Assert.IsFalse(neither.UrgencyAllotments.CanManageUrgencySettings);
        Assert.IsTrue(neither.IsUrgencyAllotmentsPanelVisible, "The panel is a read surface for everyone below the gate.");

        // ...and the picture screen by permission.settings.part_pictures. Collapsing the two would hand the
        // picture screen to somebody who only ever had the minutes (FR-020).
        Assert.IsFalse(minutesOnly.CanManageImageLocationSettings);
        Assert.IsFalse(
            minutesOnly.IsImageLocationSettingsPanelVisible,
            "A person outside the picture screen's own permission is not shown its entry point.");
        Assert.IsTrue(picturesOnly.CanManageImageLocationSettings);
        Assert.IsTrue(picturesOnly.IsImageLocationSettingsPanelVisible);
    }

    [TestMethod]
    public void SearchQuery_ReachesBothReKeyedConfigurationPanels()
    {
        var viewModel = BuildViewModel(new RecordingLocalSettingsService(), [PermissionKeys.SettingsPartPictures]);

        viewModel.SearchQuery = "allotted minutes";
        Assert.IsTrue(viewModel.IsUrgencyAllotmentsPanelVisible, "The minutes panel is keyed by Item and finds the term.");
        Assert.IsFalse(viewModel.IsImageLocationSettingsPanelVisible);

        viewModel.SearchQuery = "item images";
        Assert.IsTrue(viewModel.IsImageLocationSettingsPanelVisible, "The picture screen is keyed by Item and finds the term.");
        Assert.IsFalse(viewModel.IsUrgencyAllotmentsPanelVisible);
    }

    private static IReadOnlyList<string> DeclaredSearchAwareProperties()
    {
        var field = typeof(SettingsViewModel).GetField("s_searchAwareProperties", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, "The declared search-registration list is missing; the coverage check depends on it.");

        return (IReadOnlyList<string>)(field.GetValue(null) ?? Array.Empty<string>());
    }

    /// <summary>
    /// Whether a property's getter mentions the given metadata token. The search predicate is private, so its
    /// call site is found in the getter's IL rather than by name — which is what makes this coverage check
    /// discovery-driven and able to see the next panel that is added.
    /// </summary>
    private static bool GetterMentions(PropertyInfo property, byte[] marker)
    {
        var il = property.GetGetMethod()?.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            return false;
        }

        for (var offset = 0; offset + marker.Length <= il.Length; offset++)
        {
            var match = true;
            for (var index = 0; index < marker.Length; index++)
            {
                if (il[offset + index] != marker[index])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The picture cache is offered on the picture permission, which Developer and IT Department hold.
    /// </summary>
    /// <remarks>
    /// One gate, not two: "only IT Department and Developer may change it" is answered by the picture screen's own
    /// key, and a second permission for the same subject would be a second thing to keep in step with the seed.
    /// </remarks>
    [TestMethod]
    public void ThePictureCacheIsOfferedOnlyOnThePicturePermission()
    {
        var settings = new RecordingLocalSettingsService();
        var resolver = new FakeImageStorageConfigurationResolver();

        Assert.IsFalse(
            BuildViewModel(settings, imageStorageConfigurationResolver: resolver).IsPictureCachePanelVisible,
            "With the permission not held the section must not be offered at all.");

        Assert.IsTrue(
            BuildViewModel(settings, [PermissionKeys.SettingsPartPictures], imageStorageConfigurationResolver: resolver)
                .IsPictureCachePanelVisible);
    }

    /// <summary>
    /// A host with no picture cache does not offer the section either.
    /// </summary>
    /// <remarks>
    /// The controls would otherwise be there and do nothing, which reads as a broken screen rather than an
    /// unregistered service.
    /// </remarks>
    [TestMethod]
    public void WithoutACacheServiceTheSectionIsNotOffered()
    {
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsPartPictures]);

        Assert.IsFalse(viewModel.IsPictureCachePanelVisible);
    }

    /// <summary>
    /// Turning the cache off stores it for every user, which is what makes it follow a person to another machine.
    /// </summary>
    [TestMethod]
    public void TurningThePictureCacheOffStoresItForEveryUser()
    {
        var configuration = new FakeConfigSettingsValueService();
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsPartPictures],
            configSettingsValueService: configuration,
            imageStorageConfigurationResolver: new FakeImageStorageConfigurationResolver());

        viewModel.IsPictureCacheEnabled = false;

        var saved = configuration.SavedValues.Single();
        Assert.AreEqual(ConfigSettingKeys.ImageCacheEnabled, saved.SettingKey);
        Assert.AreEqual("all_users", saved.ScopeType, "A person-scoped row would leave every other machine unchanged.");
        Assert.AreEqual("all_users", saved.ScopeKey);
        Assert.AreEqual(false, saved.SettingValueBool);
    }

    /// <summary>The folder box is stored under its own key, so it does not collide with the toggle.</summary>
    [TestMethod]
    public void SavingTheCacheFolderStoresThePathForEveryUser()
    {
        var configuration = new FakeConfigSettingsValueService();
        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsPartPictures],
            configSettingsValueService: configuration,
            imageStorageConfigurationResolver: new FakeImageStorageConfigurationResolver());

        viewModel.PictureCacheFolderInput = @"D:\MTM Picture Cache";
        viewModel.SavePictureCacheFolderCommand.Execute(null);

        var saved = configuration.SavedValues.Single();
        Assert.AreEqual(ConfigSettingKeys.ImageCacheFolderPath, saved.SettingKey);
        Assert.AreEqual("all_users", saved.ScopeKey);
        Assert.AreEqual(@"D:\MTM Picture Cache", saved.SettingValue);
        Assert.AreEqual(@"D:\MTM Picture Cache", viewModel.PictureCacheFolderPath);
    }

    /// <summary>
    /// Copying now says what it did, and names a root it could not reach.
    /// </summary>
    /// <remarks>
    /// "Nothing to do" and "the share was not there" look identical on screen otherwise, and only one of them is a
    /// problem worth acting on.
    /// </remarks>
    [TestMethod]
    public async Task CopyingThePictureCacheNowReportsTheCountsAndAnyUnreachableRootAsync()
    {
        var cache = new FakeImageCacheSyncService
        {
            Result = new ImageCacheSyncResult(4, 2, ["waitlist pictures"])
        };

        var viewModel = BuildViewModel(
            new RecordingLocalSettingsService(),
            [PermissionKeys.SettingsPartPictures],
            imageCacheSyncService: cache,
            imageStorageConfigurationResolver: new FakeImageStorageConfigurationResolver());

        await viewModel.RefreshPictureCacheCommand.ExecuteAsync(null);

        Assert.AreEqual(1, cache.SynchronizeCallCount);
        StringAssert.Contains(viewModel.PictureCacheStatusMessage, "4");
        StringAssert.Contains(viewModel.PictureCacheStatusMessage, "2");
        StringAssert.Contains(viewModel.PictureCacheStatusMessage, "waitlist pictures");
        Assert.IsFalse(viewModel.IsPictureCacheBusy);
    }

    private static SettingsViewModel BuildViewModel(
        RecordingLocalSettingsService settings,
        string[]? heldPermissionKeys = null,
        IMockServiceRefreshClient? refreshClient = null,
        IImageStorageConfigurationResolver? imageStorageConfigurationResolver = null,
        IConfigSettingsValueService? configSettingsValueService = null,
        IImageCacheSyncService? imageCacheSyncService = null)
    {
        var viewModel = BuildViewModelWithoutLoading(
            settings,
            heldPermissionKeys,
            refreshClient,
            imageStorageConfigurationResolver,
            configSettingsValueService,
            imageCacheSyncService);

        // The stub answers from an already-completed task, so this settles synchronously and no test has to
        // sleep waiting for a gate to make up its mind.
        viewModel.LoadPermissionsAsync().GetAwaiter().GetResult();
        return viewModel;
    }

    /// <summary>
    /// Builds the screen and leaves its permission read unstarted, so a test can watch the gates before and after
    /// the page's own load.
    /// </summary>
    private static SettingsViewModel BuildViewModelWithoutLoading(
        RecordingLocalSettingsService settings,
        string[]? heldPermissionKeys = null,
        IMockServiceRefreshClient? refreshClient = null,
        IImageStorageConfigurationResolver? imageStorageConfigurationResolver = null,
        IConfigSettingsValueService? configSettingsValueService = null,
        IImageCacheSyncService? imageCacheSyncService = null)
    {
        var startupState = new StartupState();
        var computerManagement = new ComputerManagementViewModel(new FakeComputerRegistryService(), startupState);
        var urgencyAllotments = new UrgencyAllotmentEditorViewModel(
            new UrgencySettingsService(new FakeRequestItemAllottedMinutesStore()),
            new FakeRequestItemObservedTimeService());
        return new SettingsViewModel(
            new FakeThemeSelectorService(),
            settings,
            new FakeWorkCenterCatalogService(),
            new FakeDunnageTypeVisibilityCatalogService(),
            new NewRequestAlertService(settings),
            PermissionStub.Holding(heldPermissionKeys),
            new RecordingNavigationService(),
            startupState,
            computerManagement,
            urgencyAllotments,
            refreshClient ?? new FakeMockServiceRefreshClient(),
            imageStorageConfigurationResolver,
            configSettingsValueService,
            imageCacheSyncService);
    }

    /// <summary>
    /// A permission service that answers from a fixed set of held keys, which is what lets a test say exactly
    /// which gate is open rather than naming a role and hoping it maps to one.
    /// </summary>
    private sealed class PermissionStub : IPermissionService
    {
        private readonly HashSet<string> _held;

        private PermissionStub(IEnumerable<string> held) =>
            _held = new HashSet<string>(held, StringComparer.Ordinal);

        internal static PermissionStub Holding(IEnumerable<string>? heldPermissionKeys) =>
            new(heldPermissionKeys ?? []);

        internal List<string> RequestedKeys { get; } = [];

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            RequestedKeys.Add(permissionKey);
            return Task.FromResult(_held.Contains(permissionKey));
        }

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default)
        {
            var keys = permissionKeys.ToArray();
            RequestedKeys.AddRange(keys);

            return Task.FromResult<IReadOnlyDictionary<string, bool>>(
                keys.ToDictionary(key => key, _held.Contains, StringComparer.Ordinal));
        }

        public void Invalidate()
        {
        }
    }

    /// <summary>
    /// Records where this screen asked to go, so a test can prove the Administration entries navigate rather than
    /// expand.
    /// </summary>
    private sealed class RecordingNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public Microsoft.UI.Xaml.Controls.Frame? Frame
        {
            get => null;
            set { }
        }

        public bool CanGoBack => false;

        internal List<string> RequestedPageKeys { get; } = [];

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            RequestedPageKeys.Add(pageKey);
            return true;
        }

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    /// <summary>
    /// Stands in for the on-host refresh client so the panel's command can be driven without a service.
    /// </summary>
    private sealed class FakeMockServiceRefreshClient : IMockServiceRefreshClient
    {
        public int CallCount { get; private set; }

        public IReadOnlyCollection<string>? LastShapeKeys { get; private set; }

        public RefreshRequestResult Result { get; init; } =
            new() { Succeeded = true, ShapeOutcomes = new Dictionary<string, string>() };

        public Task<RefreshRequestResult> RequestRefreshAsync(
            IReadOnlyCollection<string>? shapeKeys,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastShapeKeys = shapeKeys;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeThemeSelectorService : IThemeSelectorService
    {
        public ElementTheme Theme { get; set; } = ElementTheme.Default;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SetThemeAsync(ElementTheme theme)
        {
            Theme = theme;
            return Task.CompletedTask;
        }

        public Task SetRequestedThemeAsync() => Task.CompletedTask;
    }

    private sealed class FakeDunnageTypeVisibilityCatalogService : IDunnageTypeVisibilityCatalogService
    {
        public Task<DunnageTypeVisibilityCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DunnageTypeVisibilityCatalogResult());

        public Task<IReadOnlyDictionary<string, bool>> GetVisibilityMapAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, bool>>(new Dictionary<string, bool>());

        public Task<string?> SaveVisibleDunnageTypesAsync(IReadOnlyCollection<string> visibleDunnageTypeIds, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeComputerRegistryService : IComputerRegistryService
    {
        public Task<ComputerRecord?> LookupComputerAsync(string computerName, string macAddressNormalized, CancellationToken cancellationToken = default)
            => Task.FromResult<ComputerRecord?>(null);

        public Task<ComputerRecord?> LookupComputerByMacAsync(string macAddressNormalized, CancellationToken cancellationToken = default)
            => Task.FromResult<ComputerRecord?>(null);

        public Task<IReadOnlyList<ComputerRecord>> GetAllComputersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ComputerRecord>>(Array.Empty<ComputerRecord>());

        public Task<ComputerRecord> UpsertComputerAsync(string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<ComputerRecord> UpdateComputerByMacAsync(string macAddressNormalized, string newComputerName, string hostnameNormalized, string displayName, string? description, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<ComputerRecord> UpdateComputerAsync(long id, string computerName, string hostnameNormalized, string macAddressNormalized, string displayName, string? description, bool isRegistered, CancellationToken cancellationToken = default)
            => Task.FromResult(new ComputerRecord());

        public Task<bool> DeleteComputerAsync(long id, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class RecordingLocalSettingsService : ILocalSettingsService
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public Task<T?> ReadSettingAsync<T>(string key)
        {
            if (_values.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T));
        }

        public Task SaveSettingAsync<T>(string key, T value)
        {
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task ResetSettingAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ResetAsync()
        {
            _values.Clear();
            return Task.CompletedTask;
        }

        public Task CorruptForTestAsync() => Task.CompletedTask;
    }
}
