using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

[TestClass]
public sealed class TooltipServiceTests
{
    [TestMethod]
    public void ResolvePresentation_WhenRoleIsDeveloper_UsesDeveloperModeAndKeepsFiles()
    {
        var service = CreateService("Developer");

        var presentation = service.ResolvePresentation(
            "Shell_SelectFacility_Tooltip",
            new[]
            {
                "Module_Core/Views/ShellPage.xaml",
                " ViewModels/ShellViewModel.cs ",
                "Module_Core/Views/ShellPage.xaml",
            },
            "Select the active facility.");

        Assert.IsTrue(presentation.IsDeveloperMode);
        Assert.AreEqual(2, presentation.AssociatedFiles.Count);
        Assert.AreEqual("Module_Core/Views/ShellPage.xaml", presentation.AssociatedFiles[0]);
        Assert.AreEqual("ViewModels/ShellViewModel.cs", presentation.AssociatedFiles[1]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.Text));
    }

    [TestMethod]
    public void ResolvePresentation_WhenRoleIsNormal_UsesStandardMode()
    {
        var service = CreateService("Operator");

        var presentation = service.ResolvePresentation(
            "Shell_SelectFacility_Tooltip",
            new[] { "Module_Core/Views/ShellPage.xaml" },
            "Select the active facility.");

        Assert.IsFalse(presentation.IsDeveloperMode);
        Assert.AreEqual(1, presentation.AssociatedFiles.Count);
        Assert.IsFalse(string.IsNullOrWhiteSpace(presentation.Text));
    }

    [TestMethod]
    public void ResolvePresentation_WhenResourceKeyMissing_UsesFallbackText()
    {
        var service = CreateService("Operator");

        var presentation = service.ResolvePresentation(null, null, "Fallback tooltip text");

        Assert.IsFalse(presentation.IsDeveloperMode);
        Assert.AreEqual("Fallback tooltip text", presentation.Text);
        Assert.AreEqual(0, presentation.AssociatedFiles.Count);
    }

    private static TooltipService CreateService(string role)
    {
        var startupState = new StartupState
        {
            CurrentRole = role,
        };

        return new TooltipService(startupState);
    }
}
