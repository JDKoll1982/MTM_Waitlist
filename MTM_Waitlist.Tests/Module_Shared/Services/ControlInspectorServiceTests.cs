using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

[TestClass]
public sealed class ControlInspectorServiceTests
{
    [TestMethod]
    public void Service_ControlInspectorServiceTests_TryOpenActiveDetail_WhenNotDeveloperReturnsFalse()
    {
        var navigation = new RecordingNavigationService();
        var service = CreateService("Operator", navigation);

        var opened = service.TryOpenActiveDetail();

        Assert.IsFalse(opened);
        Assert.AreEqual(0, navigation.NavigateCalls.Count);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_TryOpenActiveDetail_WhenDeveloperWithoutHoverReturnsFalse()
    {
        var navigation = new RecordingNavigationService();
        var service = CreateService("Developer", navigation);

        var opened = service.TryOpenActiveDetail();

        Assert.IsFalse(opened);
        Assert.IsFalse(service.CanOpenActiveDetail);
        Assert.AreEqual(0, navigation.NavigateCalls.Count);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_ClearActiveElement_DisablesShortcutUntilHoverReturns()
    {
        var navigation = new RecordingNavigationService();
        var service = CreateService("Developer", navigation);

        service.ClearActiveElement();

        Assert.IsFalse(service.CanOpenActiveDetail);
        Assert.IsFalse(service.TryOpenActiveDetail());
        Assert.AreEqual(0, navigation.NavigateCalls.Count);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_InspectorNavigationTarget_UsesControlInspectorDetailViewModelKey()
    {
        var navigation = new RecordingNavigationService();
        var detail = new ControlInspectorDetail
        {
            Title = "Button details",
            Summary = "Test control",
            ElementName = "SaveButton",
            ElementType = "Microsoft.UI.Xaml.Controls.Button",
            AssociatedFiles = new[] { "Module_Core/Views/ShellPage.xaml" },
        };

        var pageKey = typeof(ControlInspectorDetailViewModel).FullName!;
        var navigated = navigation.NavigateTo(pageKey, detail);

        Assert.IsTrue(navigated);
        Assert.AreEqual(1, navigation.NavigateCalls.Count);
        Assert.AreEqual(pageKey, navigation.NavigateCalls[0].PageKey);
        Assert.AreSame(detail, navigation.NavigateCalls[0].Parameter);
        Assert.IsTrue(pageKey.Contains("ControlInspectorDetailViewModel", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_ResolvePresentation_AndDetailModelCarryAssociatedFiles()
    {
        var tooltipService = new TooltipService(new FakePersonIdentity { CurrentRoleCode = "developer" });
        var presentation = tooltipService.ResolvePresentation(
            "Shell_SelectFacility_Tooltip",
            new[] { "Module_Core/Views/ShellPage.xaml", "ViewModels/ShellViewModel.cs" },
            "Select facility");

        var detail = new ControlInspectorDetail
        {
            Title = "Facility selector",
            Summary = presentation.Text,
            ResourceKey = "Shell_SelectFacility_Tooltip",
            AssociatedFiles = presentation.AssociatedFiles,
        };

        Assert.IsTrue(presentation.IsDeveloperMode);
        Assert.AreEqual(2, detail.AssociatedFiles.Count);
        Assert.AreEqual("Shell_SelectFacility_Tooltip", detail.ResourceKey);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_ExpandAssociatedFiles_IncludesModelsConvertersSharedAndCoreSupportFiles()
    {
        var expanded = ControlInspectorService.ExpandAssociatedFiles(
            element: null,
            seedFiles: new[]
            {
                "Module_Core/Views/ShellPage.xaml",
                "ViewModels/ShellViewModel.cs",
            },
            resourceKey: "Shell_SelectFacility_Tooltip");

        Assert.IsTrue(expanded.Any(static path => path.Equals("Module_Core/Views/ShellPage.xaml", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("ViewModels/ShellViewModel.cs", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("Module_Core/Views/ShellPage.xaml.cs", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Contains("/Models/", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.StartsWith("Module_Shared/", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.StartsWith("Module_Core/", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("Styles/TooltipDeveloper.xaml", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("Strings/en-us/TooltipResources.resw", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(expanded.Any(static path => path.Equals("MTM_Waitlist.Core/Services/BuildingSelectionService.cs", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_FilterAssociatedFiles_WhenHideTooltipRelatedRemovesTooltipInfrastructureOnly()
    {
        var files = new[]
        {
            "Module_Core/Views/ShellPage.xaml",
            "ViewModels/ShellViewModel.cs",
            "Module_Shared/Services/TooltipService.cs",
            "Module_Shared/Services/TooltipBehavior.cs",
            "Styles/TooltipDeveloper.xaml",
            "Strings/en-us/TooltipResources.resw",
            "MTM_Waitlist.Core/Services/BuildingSelectionService.cs",
            "MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs",
        };

        var filtered = ControlInspectorDetailViewModel
            .FilterAssociatedFiles(files, hideTooltipRelatedFiles: true)
            .ToArray();

        Assert.IsTrue(filtered.Contains("Module_Core/Views/ShellPage.xaml"));
        Assert.IsTrue(filtered.Contains("MTM_Waitlist.Core/Services/BuildingSelectionService.cs"));
        Assert.IsTrue(filtered.Contains("MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs"));
        Assert.IsFalse(filtered.Any(static path => path.Contains("Tooltip", StringComparison.OrdinalIgnoreCase)));
    }

    private static ControlInspectorService CreateService(string role, RecordingNavigationService navigation)
    {
        var person = new FakePersonIdentity { CurrentRoleCode = role };
        var tooltipService = new TooltipService(person);
        return new ControlInspectorService(person, tooltipService, navigation);
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<(string PageKey, object? Parameter)> NavigateCalls { get; } = new();

        public event NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool GoBack() => false;

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
        {
            NavigateCalls.Add((pageKey, parameter));
            return true;
        }

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }
}
