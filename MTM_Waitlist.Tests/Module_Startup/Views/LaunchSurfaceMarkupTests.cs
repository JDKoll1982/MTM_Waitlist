using System.Globalization;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Views;

/// <summary>
/// The structural half of the launch surface's requirements, read from the markup: the ones no view model can
/// answer because they are about where an element sits rather than what it holds (FR-005, FR-047, FR-048, FR-049).
/// </summary>
/// <remarks>
/// <para>
/// These read the XAML rather than a rendered window, for the same reason the waitlist card's markup checks do:
/// nothing in this suite constructs a <c>WindowEx</c>, so a rule about placement can be asserted here or nowhere.
/// What they prove is placement — that an element is a sibling of the reading area and holds a row of its own —
/// which is what "must not cover the list of work" means in markup terms.
/// </para>
/// <para>
/// <b>What they deliberately do not claim.</b> That the words are legible, that a row renders at a sensible
/// height, or that the window looks right. Those need a person or a render, and the suite says so rather than
/// implying the surface was seen.
/// </para>
/// </remarks>
[TestClass]
public sealed class LaunchSurfaceMarkupTests
{
    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly XNamespace s_xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// Every surface that can state a fault or a refusal draws it in a strip of its own, in its own row, below the
    /// area the person is reading — never laid over it (FR-005).
    /// </summary>
    /// <remarks>
    /// The rule is stated for the launch surface and again for the blocked and setup surfaces, so it is asserted
    /// once per surface: a surface that gained an overlay would otherwise pass on its neighbours' behaviour.
    /// </remarks>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_EverySurface_StatesItsMessageInAStripOfItsOwn()
    {
        string[] surfaces = ["SplashPage.xaml", "MachineSetupPage.xaml", "BlockedStatePage.xaml", "SignInPage.xaml"];

        foreach (var file in surfaces)
        {
            var (document, _) = Load(file);

            var stripElement = document
                .Descendants()
                .FirstOrDefault(element => (string?)element.Attribute((s_xaml + "Name")) is "ErrorStrip" or "MessageStrip");

            Assert.IsNotNull(stripElement, $"{file}: there is no message strip, so a fault has nowhere to appear.");

            // What FR-005 forbids is a fault covering the list of work. Sitting inside the scrolling area that
            // holds what the person is reading is what that means in markup terms; the strip is laid out beside or
            // below that area instead.
            Assert.AreEqual(
                0,
                stripElement!.Ancestors(s_presentation + "ScrollViewer").Count(),
                $"{file}: the message strip is inside the scrolling area that holds what the person is reading, so a fault would cover it (FR-005).");

            Assert.IsTrue(
                document.Descendants(s_presentation + "ScrollViewer").Any()
                    || document.Descendants().Any(element => element.Name.LocalName == "ListView"),
                $"{file}: the surface holds nothing scrollable, so this check would pass without proving the strip sits outside the reading area.");

            Assert.IsFalse(
                stripElement.AncestorsAndSelf().Any(ancestor => ancestor.Name == s_presentation + "Canvas"),
                $"{file}: the strip is inside a Canvas, so it is positioned over what the person is reading rather than laid out beside it.");

            Assert.IsFalse(
                stripElement.AncestorsAndSelf().Any(ancestor => ancestor.Name == s_presentation + "Popup" || ancestor.Name == s_presentation + "Flyout"),
                $"{file}: the strip is inside a popup or flyout, which covers the surface it explains.");
        }
    }

    /// <summary>
    /// The row the strip holds exists in the grid that draws the surface, so the strip is actually placed rather
    /// than named in a row the grid does not declare (FR-005).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_EverySurface_DeclaresTheRowItsStripSitsIn()
    {
        (string File, string Strip, string StripRow)[] surfaces =
        [
            ("SplashPage.xaml", "ErrorStrip", "3"),
            ("MachineSetupPage.xaml", "MessageStrip", "2"),
            ("BlockedStatePage.xaml", "MessageStrip", "2"),
            ("SignInPage.xaml", "MessageStrip", "2"),
        ];

        foreach (var (file, strip, stripRow) in surfaces)
        {
            var (document, _) = Load(file);
            var row = int.Parse(stripRow, CultureInfo.InvariantCulture);

            var declaredRows = document
                .Descendants(s_presentation + "Grid")
                .Where(grid => grid.Element(s_presentation + "Grid.RowDefinitions") is not null)
                .Max(grid => grid.Element(s_presentation + "Grid.RowDefinitions")!
                    .Elements(s_presentation + "RowDefinition")
                    .Count());

            Assert.IsTrue(
                declaredRows > row,
                $"{file}: no grid declares a row {stripRow}, so {strip} is not placed anywhere and a fault would not show.");
        }
    }

    /// <summary>
    /// The launch surface draws the operations that are running, each as its own row of a list, rather than one
    /// "current step" that stands for the rest (FR-047, US6).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_TheLaunchSurface_DrawsEveryOperationAsItsOwnRowRatherThanOneCurrentStep()
    {
        var (document, _) = Load("SplashPage.xaml");

        var list = document
            .Descendants(s_presentation + "ListView")
            .FirstOrDefault(element => element.Element(s_presentation + "ListView.ItemTemplate") is not null);

        Assert.IsNotNull(
            list,
            "The launch surface no longer draws its operations through a list, so there is no row per operation (FR-047).");

        Assert.IsNotNull(
            (string?)list!.Attribute("ItemsSource"),
            "The launch surface's operation list is not bound to the feed, so it would render no rows at all.");

        var template = list.Element(s_presentation + "ListView.ItemTemplate")!.Element(s_presentation + "DataTemplate");

        Assert.IsNotNull(template, "The operation list has no item template, so an operation has no row shape.");

        Assert.IsTrue(
            template!.Descendants(s_presentation + "TextBlock").Any(),
            "The operation row draws no text, so a running operation would be an empty line.");

        // One current step would be a single TextBlock bound to the view model rather than a templated list: the
        // template is what makes several operations renderable at the same time.
        var singleCurrentStep = document
            .Descendants(s_presentation + "TextBlock")
            .FirstOrDefault(text => (string?)text.Attribute("Text") == "{x:Bind ViewModel.CurrentStep, Mode=OneWay}");

        Assert.IsNull(
            singleCurrentStep,
            "The launch surface is back to one bound current step, which hides every other operation that is running (FR-047).");
    }

    /// <summary>
    /// The people who signed in here before are drawn to the right of the form, separated from it by a rule of
    /// their own — not above it, not behind a button, and not inside the form's own scrolling area (FR-048).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_TheSignInSurface_PutsThePreviousPeopleToTheRightOfTheFormBehindADivider()
    {
        var (document, _) = Load("SignInPage.xaml");

        var panel = document
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute((s_xaml + "Name")) == "PreviousPeoplePanel");

        Assert.IsNotNull(panel, "The sign-in surface no longer carries the previous-people panel (FR-048).");

        var panelHost = panel!.Ancestors(s_presentation + "ScrollViewer").FirstOrDefault();

        Assert.IsNotNull(
            panelHost,
            "The previous-people panel is not inside its own scrolling host, so a long list would be clipped.");

        Assert.AreEqual(
            "2",
            (string?)panelHost!.Attribute("Grid.Column"),
            "The previous people are no longer in the right-hand column, so they sit with the form rather than beside it.");

        var formHost = document
            .Descendants(s_presentation + "ScrollViewer")
            .FirstOrDefault(element => (string?)element.Attribute("Grid.Column") == "0");

        Assert.IsNotNull(formHost, "The sign-in form is no longer in the left-hand column, so the divider has nothing to divide.");

        var divider = document
            .Descendants(s_presentation + "Border")
            .FirstOrDefault(border => (string?)border.Attribute("Grid.Column") == "1" && (string?)border.Attribute("Width") == "1");

        Assert.IsNotNull(
            divider,
            "The rule between the form and the previous people is gone, so the two read as one column (FR-048).");
    }

    /// <summary>
    /// Choosing one of the previous people fills in the user name and nothing else: the list offers names to type
    /// beside, never a way past the PIN (FR-014, FR-048).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_ThePreviousPeople_OfferNoWayPastThePin()
    {
        var (document, _) = Load("SignInPage.xaml");

        var panel = document
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute((s_xaml + "Name")) == "PreviousPeoplePanel");

        Assert.IsNotNull(panel, "The sign-in surface no longer carries the previous-people panel (FR-048).");

        var credentialish = panel!
            .Descendants()
            .Where(element => element.Name.LocalName == "PasswordBox")
            .ToList();

        Assert.AreEqual(
            0,
            credentialish.Count,
            $"A previous person's card carries a credential control, which FR-014 forbids: {credentialish.FirstOrDefault()}");

        var submitCommands = panel
            .Descendants(s_presentation + "Button")
            .Where(button => ((string?)button.Attribute("Command"))?.Contains("SignInCommand", StringComparison.Ordinal) == true)
            .ToList();

        Assert.AreEqual(
            0,
            submitCommands.Count,
            "A previous person's card can submit the sign-in, so choosing one would be a way in without a PIN.");

        // The card has to hand the name to the form, or choosing one would do nothing at all.
        var clickHandlers = panel
            .Descendants(s_presentation + "Button")
            .Where(button => button.Attribute("Click") is not null)
            .ToList();

        Assert.AreEqual(
            1,
            clickHandlers.Count,
            "A previous person's card does not fill in the user name, which is the whole of what choosing one does.");
    }

    /// <summary>
    /// The setup stage shows the computer's own name as text with no field behind it, because a field — even a
    /// read-only one a person can focus — is what lets a name be typed (FR-049).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_TheSetupStage_ShowsTheComputersOwnNameWithNoFieldBehindIt()
    {
        var (document, _) = Load("MachineSetupPage.xaml");

        var nameText = document
            .Descendants()
            .FirstOrDefault(element => (string?)element.Attribute((s_xaml + "Name")) == "ComputerNameText");

        Assert.IsNotNull(nameText, "The setup stage no longer shows the computer's own name (FR-049).");

        Assert.AreEqual(
            "TextBlock",
            nameText!.Name.LocalName,
            "The computer's own name is drawn by something other than a TextBlock, so it is no longer plain text.");

        Assert.IsTrue(
            ((string?)nameText.Attribute("Text"))?.Contains("ComputerName", StringComparison.Ordinal) == true,
            "The setup stage's name line is not bound to the computer's own name.");

        var identityPanel = nameText.Ancestors(s_presentation + "StackPanel").First();

        var editable = identityPanel
            .Descendants()
            .Where(element => element.Name.LocalName is "TextBox" or "NumberBox")
            .ToList();

        Assert.AreEqual(
            1,
            editable.Count,
            $"The setup stage offers {editable.Count} editable fields for this computer's identity; only the location is a person's to supply (FR-049).");

        Assert.AreEqual(
            "LocationBox",
            (string?)editable[0].Attribute((s_xaml + "Name")),
            "The one editable field for this computer's identity is not the location.");
    }

    /// <summary>
    /// Every surface the launch shows is a page, so each is a stage of the one window rather than a window of its
    /// own (FR-016, FR-017).
    /// </summary>
    [TestMethod]
    public void UI_LaunchSurfaceMarkupTests_EveryLaunchSurface_IsAPageRatherThanAWindow()
    {
        string[] surfaces =
        [
            "SplashPage.xaml",
            "SignInPage.xaml",
            "MachineSetupPage.xaml",
            "BlockedStatePage.xaml",
        ];

        foreach (var file in surfaces)
        {
            var (document, root) = Load(file);

            Assert.AreEqual(
                "Page",
                root.Name.LocalName,
                $"{file} is a {root.Name.LocalName}, so the launch would open a window rather than move a stage (FR-017).");

            Assert.IsFalse(
                document.Descendants(s_presentation + "Window").Any(),
                $"{file} declares a Window of its own.");
        }
    }

    private static (XDocument Document, XElement Root) Load(string fileName)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), "Module_Startup", "Views", fileName);

        Assert.IsTrue(File.Exists(path), $"The launch surface {fileName} is missing.");

        var document = XDocument.Load(path);

        return (document, document.Root!);
    }
}
