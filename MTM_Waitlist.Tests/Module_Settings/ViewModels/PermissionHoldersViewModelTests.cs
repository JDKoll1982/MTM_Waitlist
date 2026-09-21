using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models.UserManagement;
using MTM_Waitlist.Module_Core.Permissions;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.ViewModels;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The who-holds-this view (T063, FR-075 to FR-079, SC-014).
/// </summary>
/// <remarks>
/// The two reads behind it are the store's, and the live change-set suite proves the store half. What this suite
/// proves is the answer the reader is shown: the roles, only the people who differ, a feature nobody holds stated
/// in words, a switched-off holder marked, and that the view writes nothing at all.
/// </remarks>
[TestClass]
public sealed class PermissionHoldersViewModelTests
{
    [TestMethod]
    public async Task TheRolesComeFromTheBaselinesReadFromTheStore_RatherThanFromASecondCopy()
    {
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(["it_department", "plant_manager"], []),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        Assert.AreEqual(1, service.HolderReads, "The roles are read, once, for the chosen feature.");
        Assert.IsTrue(
            PermissionKeys.All.Contains(service.LastKeyRead),
            "The read asks about a declared permission, and asks the store rather than a copy kept beside the view.");
        Assert.AreEqual(
            2,
            viewModel.View!.RoleTexts.Count,
            "The roles the view shows are the ones the read returned: it holds no list of its own to fall back on.");
        Assert.IsTrue(viewModel.View.RoleTexts.All(text => !string.IsNullOrWhiteSpace(text)));
        Assert.IsTrue(viewModel.HasRoles);
        Assert.IsFalse(viewModel.NobodyHoldsIt);
    }

    [TestMethod]
    public async Task OnlyThePeopleWhoDiffer_AreNamedIndividually()
    {
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(
                ["plant_manager"],
                [
                    new PermissionHolder(11, "Ada Granted", "6229", "setup", IsSwitchedOff: false, IsGranted: true),
                    new PermissionHolder(12, "Bo Denied", "6230", "setup", IsSwitchedOff: false, IsGranted: false),
                ]),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.View!.People.Count, "Only the people who differ are listed; the rest inherit.");
        Assert.IsTrue(viewModel.HasPeople);
    }

    [TestMethod]
    public async Task APersonWhoDiffersInTheDeniedDirection_IsNamedAndMarked()
    {
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(
                ["plant_manager"],
                [new PermissionHolder(12, "Bo Denied", "6230", "setup", IsSwitchedOff: false, IsGranted: false)]),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        var person = viewModel.View!.People.Single();

        Assert.AreEqual("Bo Denied", person.DisplayName);
        Assert.IsFalse(person.IsGranted, "The denied direction is named as plainly as the granted one (FR-076).");
        Assert.AreEqual("Permissions_Holders.Denied".GetLocalized(), person.MarkText);
    }

    [TestMethod]
    public async Task ASwitchedOffHolder_IsListedAndMarked()
    {
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(
                ["plant_manager"],
                [new PermissionHolder(13, "Cy Off", "6231", "setup", IsSwitchedOff: true, IsGranted: true)]),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        var person = viewModel.View!.People.Single();

        Assert.IsTrue(person.IsSwitchedOff, "A switched-off holder is listed rather than hidden (FR-079).");
        Assert.IsFalse(string.IsNullOrWhiteSpace(person.SwitchedOffText), "And marked as switched off.");
        Assert.IsTrue(person.IsGranted, "They still hold it, which is exactly why the mark matters.");
    }

    [TestMethod]
    public async Task AFeatureNobodyHolds_IsStatedInWords()
    {
        var service = new FakePermissionAdministrationService { Holders = new PermissionHolders([], []) };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.NobodyHoldsIt, "Nobody holds it is a state the view states (FR-078).");
        Assert.IsTrue(viewModel.View!.NobodyHoldsIt);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.NobodyText), "And the sentence is there to show.");
        Assert.IsFalse(viewModel.HasRoles);
        Assert.IsFalse(viewModel.HasPeople);
    }

    [TestMethod]
    public async Task NobodyHoldsIt_IsDistinctFromNobodyDiffering()
    {
        // A feature several roles hold, with no exceptions: nobody DIFFERS, but people hold it.
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(["plant_manager", "developer"], []),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        Assert.IsFalse(
            viewModel.NobodyHoldsIt,
            "A feature its roles' baselines give is held, even when no person differs from their own role.");
        Assert.IsFalse(viewModel.HasPeople);
    }

    [TestMethod]
    public async Task TheView_WritesNothingAtAll()
    {
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(
                ["plant_manager"],
                [new PermissionHolder(11, "Ada Granted", "6229", "setup", IsSwitchedOff: false, IsGranted: true)]),
        };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        viewModel.OpenPersonCommand.Execute(viewModel.View!.People.Single());

        Assert.AreEqual(1, service.HolderReads, "Reading is all this view does.");
        Assert.AreEqual(0, service.Applies, "It never writes a change set (FR-077).");
        Assert.AreEqual(0, service.Reversals, "And it never reverses one.");
    }

    [TestMethod]
    public async Task ChoosingADifferingPerson_OpensTheirOwnPage()
    {
        var navigation = new RecordingNavigationService();
        var service = new FakePermissionAdministrationService
        {
            Holders = new PermissionHolders(
                ["plant_manager"],
                [new PermissionHolder(11, "Ada Granted", "6229", "setup", IsSwitchedOff: false, IsGranted: true)]),
        };

        var viewModel = Build(service: service, navigation: navigation);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        viewModel.OpenPersonCommand.Execute(viewModel.View!.People.Single());

        Assert.AreEqual(
            PermissionHoldersViewModel.PersonPageViewModelName,
            navigation.LastPageKey,
            "A differing person opens their own page, which is where they are changed (FR-077).");
        Assert.AreEqual(11L, navigation.LastParameter);
    }

    [TestMethod]
    public async Task TheFeaturePicker_OffersEveryDeclaredPermission()
    {
        var viewModel = Build(service: new FakePermissionAdministrationService());
        viewModel.LoadFeatures();

        CollectionAssert.AreEquivalent(
            PermissionKeys.All.ToArray(),
            viewModel.Features.Select(feature => feature.Key).ToArray(),
            "Every declared permission can be asked about, and none is invented (FR-047).");
        Assert.IsNotNull(viewModel.SelectedFeature, "One is chosen, so the view opens on an answer rather than on nothing.");
    }

    [TestMethod]
    public async Task AnUnreachableStore_IsStatedRatherThanShownAsNobodyHoldingIt()
    {
        var service = new FakePermissionAdministrationService { ThrowOnHolderRead = new InvalidOperationException("down") };

        var viewModel = Build(service: service);
        viewModel.LoadFeatures();
        await viewModel.LoadAsync();

        Assert.IsTrue(viewModel.IsStoreUnavailable);
        Assert.IsNull(viewModel.View, "No answer is invented, and 'nobody holds it' is not claimed.");
        Assert.IsFalse(viewModel.NobodyHoldsIt, "An unread answer is not the same as an empty one.");
        Assert.AreEqual(viewModel.UnavailableText, viewModel.MessageText);
        Assert.IsFalse(viewModel.IsBusy, "The view does not freeze while it loads (FR-114).");
    }

    private static PermissionHoldersViewModel Build(
        FakePermissionAdministrationService? service = null,
        RecordingNavigationService? navigation = null) =>
        new(service ?? new FakePermissionAdministrationService(), navigation ?? new RecordingNavigationService());

    private sealed class FakePermissionAdministrationService : IPermissionAdministrationService
    {
        internal PermissionHolders Holders { get; set; } = new([], []);

        internal Exception? ThrowOnHolderRead { get; set; }

        internal int HolderReads { get; private set; }

        internal string? LastKeyRead { get; private set; }

        internal int Applies { get; private set; }

        internal int Reversals { get; private set; }

        public Task<IReadOnlyList<PermissionValueRow>> GetForPersonAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PermissionValueRow>>([]);

        public Task<PermissionChangeResult> ApplyAsync(
            long userId,
            IReadOnlyList<PermissionChange> changes,
            CancellationToken cancellationToken = default)
        {
            Applies++;
            return Task.FromResult(PermissionChangeResult.Succeeded());
        }

        public Task<PermissionChangeResult> ReverseLastSaveAsync(
            long userId,
            bool restoreDespiteMovedValue = false,
            CancellationToken cancellationToken = default)
        {
            Reversals++;
            return Task.FromResult(PermissionChangeResult.Succeeded());
        }

        public Task<PermissionHolders> GetHoldersAsync(string permissionKey, CancellationToken cancellationToken = default)
        {
            HolderReads++;
            LastKeyRead = permissionKey;

            return ThrowOnHolderRead is not null
                ? Task.FromException<PermissionHolders>(ThrowOnHolderRead)
                : Task.FromResult(Holders);
        }
    }

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

        internal string? LastPageKey { get; private set; }

        internal object? LastParameter { get; private set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            LastPageKey = pageKey;
            LastParameter = parameter;
            return true;
        }

        public bool GoBack() => false;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
