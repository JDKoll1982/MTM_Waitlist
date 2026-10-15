using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Models;
using MTM_Waitlist.Module_Shared.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Shared;

/// <summary>
/// Every tooltip the application shows comes from the one tooltip implementation, and nothing sets the platform's
/// tooltip behind its back.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is a rule rather than a preference.</b> The two paths are not two styles of the same thing. The
/// custom tooltip resolves its text from the tooltip resource maps, gives a developer the richer variant with the
/// shortcut hint on it, and — the part that matters to a person debugging the app — registers the control with the
/// control inspector, which is what makes Ctrl+Alt+Right-Click able to describe it. A control whose tooltip is set
/// with <c>ToolTipService.ToolTip</c> gets none of that: it looks like the rest of the application and is invisible
/// to the inspector.
/// </para>
/// <para>
/// <b>The value-echo tooltips are converted too, and keep their text.</b> Several tooltips exist to hold a value in
/// one piece where its control truncates or scrolls it — a person's name, a part number, a card's action. Those
/// carry their text as the behaviour's fallback rather than as a resource key, because the value is the message;
/// what changed is the path it travels, not what it says.
/// </para>
/// </remarks>
[TestClass]
public sealed class TooltipCoverageTests
{
    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    /// <summary>The files that used to set the platform tooltip, each of which now declares the behaviour.</summary>
    private static readonly string[] s_convertedFiles =
    [
        Path.Combine("Module_Core", "Views", "ShellPage.xaml"),
        Path.Combine("Module_Waitlist", "Controls", "WaitlistLineCardView.xaml"),
        Path.Combine("Module_Settings", "Views", "UserManagementPage.xaml"),
        Path.Combine("Module_Settings", "Views", "ImageOverrideEditorControl.xaml"),
        Path.Combine("Module_Setup", "Views", "SetupDunnageImageSearchDialog.xaml"),
    ];

    /// <summary>Every XAML file in the tree, excluding build output.</summary>
    private static IEnumerable<string> XamlFiles()
    {
        var directories = new[] { "Module_Core", "Module_Shared", "Module_Settings", "Module_Setup", "Module_Startup", "Module_Waitlist" };

        foreach (var directory in directories)
        {
            var path = Path.Combine(Root, directory);
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.xaml", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    [TestMethod]
    public void UI_TooltipCoverageTests_NoMarkupSetsThePlatformsTooltip()
    {
        var offenders = new List<string>();

        foreach (var file in XamlFiles())
        {
            var text = File.ReadAllText(file);
            if (text.Contains("ToolTipService.ToolTip", StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(Root, file));
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "A tooltip set with ToolTipService.ToolTip resolves neither the tooltip resources nor the developer "
                + "variant, and leaves the control invisible to the control inspector. Use shared:TooltipBehavior "
                + "instead. Files: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void UI_TooltipCoverageTests_EveryConvertedControlDeclaresTheBehaviour()
    {
        foreach (var relative in s_convertedFiles)
        {
            var path = Path.Combine(Root, relative);
            Assert.IsTrue(File.Exists(path), $"{relative} is gone, so this guard is watching nothing.");

            var document = XDocument.Load(path);
            var wired = document
                .Descendants()
                .Count(element => element.Attributes().Any(attribute =>
                    attribute.Name.LocalName is "TooltipBehavior.ResourceKey" or "TooltipBehavior.FallbackText"));

            Assert.AreNotEqual(
                0,
                wired,
                $"{relative} declares no tooltip through the behaviour, so its tooltips have gone back to the "
                    + "platform's or been dropped.");
        }
    }

    /// <summary>The one tooltip that had to gain a resource key, in both maps.</summary>
    [TestMethod]
    public void UI_TooltipCoverageTests_TheConvertedStaticTooltipHasAResourceKeyInBothMaps()
    {
        foreach (var map in new[] { "TooltipResources.resw", "TooltipResources.developer.resw" })
        {
            var path = Path.Combine(Root, "Strings", "en-us", map);
            var document = XDocument.Load(path);

            var entry = document
                .Descendants("data")
                .FirstOrDefault(element => (string?)element.Attribute("name") == "Setup_DunnageShowAll.Tooltip");

            Assert.IsNotNull(entry, $"{map} is missing the key the converted dunnage toggle resolves through.");
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(entry!.Element("value")?.Value),
                $"{map} holds the key with no text, so the tooltip would fall back to a placeholder.");
        }
    }

    /// <summary>The window's view model is loaded with a detail, because there is no frame to navigate.</summary>
    [TestMethod]
    public void ViewModel_TooltipCoverageTests_LoadFillsTheDetailsAndKeepsTheFileListOutOfTheSections()
    {
        var viewModel = new ControlInspectorDetailViewModel();
        var detail = new ControlInspectorDetail
        {
            Title = "Save button",
            Summary = "Test control",
            ElementType = "Microsoft.UI.Xaml.Controls.Button",
            AutomationId = "SaveButton",
            AssociatedFiles = new[] { "Module_Core/Views/ShellPage.xaml" },
            Sections = new[]
            {
                new ControlInspectorSection
                {
                    Title = "Associated files",
                    Fields = new[] { new ControlInspectorField { Label = "File", Value = "Module_Core/Views/ShellPage.xaml" } },
                },
                new ControlInspectorSection
                {
                    Title = "Identity",
                    Fields = new[] { new ControlInspectorField { Label = "Name", Value = "Save" } },
                },
            },
        };

        viewModel.Load(detail);

        Assert.AreSame(detail, viewModel.Detail);
        Assert.AreEqual("Save button", viewModel.Detail!.Title);
        Assert.AreEqual("Microsoft.UI.Xaml.Controls.Button", viewModel.ElementTypeLabel);
        Assert.AreEqual("SaveButton", viewModel.AutomationIdLabel);

        // The file list is drawn by its own card, so the section list must not repeat it.
        Assert.AreEqual(1, viewModel.Sections.Count);
        Assert.AreEqual("Identity", viewModel.Sections[0].Title);

        Assert.IsTrue(
            viewModel.VisibleAssociatedFiles.Contains("Module_Core/Views/ShellPage.xaml"),
            "The window's file list lost the files the tooltip declared.");
    }

    /// <summary>A window loaded with no details states that rather than throwing.</summary>
    [TestMethod]
    public void ViewModel_TooltipCoverageTests_LoadWithNoDetailLeavesEmptyLabelsRatherThanThrowing()
    {
        var viewModel = new ControlInspectorDetailViewModel();

        viewModel.Load(null);

        Assert.IsNull(viewModel.Detail);
        Assert.AreEqual("Unknown element", viewModel.ElementTypeLabel);
        Assert.AreEqual("(no automation id)", viewModel.AutomationIdLabel);
        Assert.AreEqual(0, viewModel.Sections.Count);
        Assert.AreEqual(0, viewModel.VisibleAssociatedFiles.Count);
    }
}
