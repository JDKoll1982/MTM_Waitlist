using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

[TestClass]
public sealed class ControlInspectorServiceTests
{
    [TestMethod]
    public void Service_ControlInspectorServiceTests_TryOpenActiveDetail_WhenNotDeveloperReturnsFalse()
    {
        var presenter = new RecordingPresenter();
        var service = CreateService("Operator", presenter);

        var opened = service.TryOpenActiveDetail();

        Assert.IsFalse(opened);
        Assert.AreEqual(0, presenter.Presented.Count);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_TryOpenActiveDetail_WhenDeveloperWithoutHoverReturnsFalse()
    {
        var presenter = new RecordingPresenter();
        var service = CreateService("Developer", presenter);

        var opened = service.TryOpenActiveDetail();

        Assert.IsFalse(opened);
        Assert.IsFalse(service.CanOpenActiveDetail);
        Assert.AreEqual(0, presenter.Presented.Count);
    }

    [TestMethod]
    public void Service_ControlInspectorServiceTests_ClearActiveElement_DisablesShortcutUntilHoverReturns()
    {
        var presenter = new RecordingPresenter();
        var service = CreateService("Developer", presenter);

        service.ClearActiveElement();

        Assert.IsFalse(service.CanOpenActiveDetail);
        Assert.IsFalse(service.TryOpenActiveDetail());
        Assert.AreEqual(0, presenter.Presented.Count);
    }

    /// <summary>
    /// The inspector is presented, not navigated to: a page behind a modal dialog can never be seen, which is
    /// where a hovered control very often is.
    /// </summary>
    /// <remarks>
    /// This is asserted against the markup and the seam rather than by driving a hover, because the hover that
    /// arms the shortcut comes from a real pointer over a real window. What is pinned here is the shape that made
    /// the fix possible: the service's third dependency is the presenter, and the page it used to navigate to is
    /// gone from the tree along with every mention of its page key.
    /// </remarks>
    [TestMethod]
    public void Service_ControlInspectorServiceTests_InspectorIsPresentedInItsOwnWindowRatherThanNavigatedTo()
    {
        var serviceType = typeof(ControlInspectorService);
        var dependencies = serviceType
            .GetConstructors()
            .SelectMany(static constructor => constructor.GetParameters())
            .Select(static parameter => parameter.ParameterType)
            .ToArray();

        Assert.IsTrue(
            dependencies.Contains(typeof(IControlInspectorDetailPresenter)),
            "The inspector service no longer asks for a presenter, so it has gone back to whatever surface it can "
                + "reach from a library - which was a page.");

        Assert.IsFalse(
            dependencies.Any(static type => type.Name == "INavigationService"),
            "The inspector service navigates again, so its details are shown on a page that a modal dialog hides.");

        var root = RepositoryPatternScan.FindRepositoryRoot();

        Assert.IsTrue(
            File.Exists(Path.Combine(root, "Module_Shared", "Views", "ControlInspectorDetailWindow.xaml")),
            "The inspector's window is gone.");

        Assert.IsFalse(
            File.Exists(Path.Combine(root, "Module_Shared", "Views", "ControlInspectorDetailPage.xaml")),
            "The inspector's page is back: a page cannot be shown over the modal dialog that raised it.");

        var window = File.ReadAllText(Path.Combine(root, "Module_Shared", "Views", "ControlInspectorDetailWindow.xaml"));

        StringAssert.Contains(window, "<windowex:WindowEx", "The inspector's surface is not a window.");
        StringAssert.Contains(
            window,
            "ControlInspectorDetailWindow_CloseButton",
            "The window has no close path of its own, and it has no navigation stack to go back along.");
        Assert.IsFalse(
            window.Contains("ViewModel.BackCommand", StringComparison.Ordinal),
            "The window still binds a Back command, which belonged to the page's navigation stack.");
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

    private static ControlInspectorService CreateService(string role, RecordingPresenter presenter)
    {
        var person = new FakePersonIdentity { CurrentRoleCode = role };
        var tooltipService = new TooltipService(person);
        return new ControlInspectorService(person, tooltipService, presenter);
    }

    /// <summary>
    /// Stands in for the window that shows the details. It records what it was asked to present, so a test can say
    /// whether the shortcut reached the surface and what it carried.
    /// </summary>
    private sealed class RecordingPresenter : IControlInspectorDetailPresenter
    {
        public List<ControlInspectorDetail> Presented { get; } = new();

        /// <summary>What <see cref="Present"/> answers, so a surface that could not be shown can be driven.</summary>
        public bool Result { get; set; } = true;

        public bool Present(ControlInspectorDetail detail)
        {
            Presented.Add(detail);
            return Result;
        }
    }
}
