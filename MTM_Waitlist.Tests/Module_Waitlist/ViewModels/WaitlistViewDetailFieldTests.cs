using System.Reflection;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// US1 check 2 (<c>contracts/verification-gates.md</c> G4 #1b). The request detail page must render no
/// attribute that no source produces, and its label/value helper must not be able to re-introduce a
/// placeholder through a default parameter.
/// </summary>
[TestClass]
public sealed class WaitlistViewDetailFieldTests
{
    /// <summary>
    /// Labels the detail page invented: nothing in the request, the active job or the receiving store
    /// produces a tipping strategy or an allowed-categories list.
    /// </summary>
    private static readonly string[] s_inventedLabels = ["Tipping strategy", "Allowed categories"];

    private static readonly string[] s_inventedStatusValues =
    [
        "Pending",
        "Pending assignment",
        "Pending Quality review",
        "Pending pickup",
    ];

    /// <summary>
    /// Every catalogued Item, so the audit covers each shape a request can actually be raised with. The
    /// fixtures are driven by the Item code the store carries: there is no type/subtype pair to drive them
    /// with any more (FR-003, FR-004).
    /// </summary>
    private static IReadOnlyList<string> EveryDetailShape { get; } =
        RequestItemCatalog.Items.Select(item => item.Id).ToArray();

    [TestMethod]
    public void FieldValue_ExposesNoFallbackDefault()
    {
        var helper = typeof(WaitlistViewDetailViewModel)
            .GetMethod("FieldValue", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(helper, "The label/value helper moved. Update this check rather than deleting it.");

        var fallback = helper.GetParameters().FirstOrDefault(parameter => parameter.HasDefaultValue);

        Assert.IsNull(
            fallback,
            $"FieldValue still exposes a fallback default ('{fallback?.Name}'), which lets a placeholder value re-enter through the helper.");
    }

    [TestMethod]
    public void LoadedSections_RenderNoInventedAttributeAndNoAbsencePlaceholder()
    {
        foreach (var itemCode in EveryDetailShape)
        {
            var viewModel = BuildLoadedViewModel(itemCode);
            var shape = itemCode;

            Assert.IsTrue(viewModel.Item is not null, $"{shape}: the detail page resolved no item, so this check proved nothing.");

            var fields = viewModel.TemplateSections.SelectMany(section => section.Fields).ToList();

            foreach (var field in fields)
            {
                Assert.IsFalse(
                    s_inventedLabels.Contains(field.Label, StringComparer.OrdinalIgnoreCase),
                    $"{shape}: the detail page renders the invented '{field.Label}' attribute.");

                Assert.IsFalse(
                    s_inventedStatusValues.Contains(field.Value, StringComparer.OrdinalIgnoreCase),
                    $"{shape}: the detail page renders the invented status '{field.Value}'.");

                Assert.IsFalse(
                    field.Value.StartsWith("Pending", StringComparison.OrdinalIgnoreCase),
                    $"{shape}: the detail page renders '{field.Label}' as '{field.Value}', which no source produces.");

                Assert.IsFalse(
                    FabricatedValueGuard.IsFabricatedLooking(field.Value),
                    $"{shape}: the detail page renders the fabricated-looking value '{field.Value}' for '{field.Label}'.");
            }
        }
    }

    [TestMethod]
    public void LoadedSections_CarryNoFieldWithoutAValue()
    {
        foreach (var itemCode in EveryDetailShape)
        {
            var viewModel = BuildLoadedViewModel(itemCode);

            foreach (var section in viewModel.TemplateSections)
            {
                foreach (var field in section.Fields)
                {
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(field.Value),
                        $"{itemCode}: section '{section.Title}' renders the labelled shell '{field.Label}' with no value.");
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(field.Label),
                        $"{itemCode}: section '{section.Title}' renders a value with no label.");
                }
            }
        }
    }

    private static WaitlistViewDetailViewModel BuildLoadedViewModel(string itemCode)
    {
        var requestService = new WaitlistRequestService();
        var submit = requestService
            .SubmitAsync(
                new WaitlistRequestDraft
                {
                    Building = "Expo Drive",
                    WorkCenter = "Expo Line 7",
                    Category = RequestItemCatalog.FindById(itemCode)!.Category.ToString(),
                    Item = itemCode,
                    InputValue = "Skid 4471 is on the wrong dock",
                    ActiveSetupJobId = "JOB-9001",
                    WorkCenterName = "Expo Line 7",
                    RequesterEmployeeNumber = "6331",
                    RequesterEmployeeName = "Dana Whitfield",
                },
                allowDuplicate: true)
            .GetAwaiter()
            .GetResult();

        Assert.IsNotNull(submit.Request, $"{itemCode}: submitting the fixture request failed.");

        var order = WaitlistViewViewModel.CreateSessionOrder(submit.Request!);
        var viewModel = new WaitlistViewDetailViewModel(
            new WaitlistTestNavigationService(),
            new WaitlistTestBuildingSelectionService(),
            requestService: requestService);

        viewModel.OnNavigatedTo(order.Id);
        return viewModel;
    }
}

/// <summary>A navigation service that records nothing and navigates nowhere.</summary>
internal sealed class WaitlistTestNavigationService : INavigationService
{
    /// <inheritdoc />
    public event NavigatedEventHandler? Navigated
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool CanGoBack => true;

    /// <inheritdoc />
    public Frame? Frame { get; set; }

    /// <summary>How many times the back action was asked for.</summary>
    public int GoBackCallCount { get; private set; }

    /// <summary>How many times a forward navigation was asked for.</summary>
    public int NavigateToCallCount { get; private set; }

    /// <summary>The page key of the last forward navigation, when there was one.</summary>
    public string? LastRequestedPageKey { get; private set; }

    /// <summary>The parameter of the last forward navigation, when there was one.</summary>
    public object? LastRequestedParameter { get; private set; }

    /// <inheritdoc />
    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        NavigateToCallCount++;
        LastRequestedPageKey = pageKey;
        LastRequestedParameter = parameter;
        return true;
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        GoBackCallCount++;
        return true;
    }

    /// <inheritdoc />
    public void SetListDataItemForNextConnectedAnimation(object item)
    {
    }
}

/// <summary>A building selection that reports one building and never changes it.</summary>
internal sealed class WaitlistTestBuildingSelectionService : IBuildingSelectionService
{
    /// <inheritdoc />
    public event EventHandler? BuildingChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Buildings => ["Expo Drive"];

    /// <inheritdoc />
    public string SelectedBuilding { get; set; } = "Expo Drive";
}
