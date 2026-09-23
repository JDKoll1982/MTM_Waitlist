using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The card must not let the image-location service's "no image available" placeholder replace the image the
/// row already carries. This is the rule that stops the tile degrading the first time anything initializes the
/// image service mid-session.
/// </summary>
[TestClass]
public sealed class RequestImagePathPolicyTests
{
    private string _directory = string.Empty;
    private string _picturePath = string.Empty;
    private string _standInPath = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = ImageHeaderFixtures.CreateTemporaryDirectory();
        _picturePath = Path.Combine(_directory, "picture.png");
        _standInPath = Path.Combine(_directory, "stand-in.png");
    }

    [TestCleanup]
    public void TestCleanup() => ImageHeaderFixtures.DeleteTemporaryDirectory(_directory);

    /// <summary>
    /// The string half of the rule: a path that is not the resolver's placeholder is worth taking.
    /// <see cref="APathThatCarriesNoPictureIsNotTaken"/> covers the other half, which is the file behind it.
    /// </summary>
    [TestMethod]
    public void ARealResolvedPathIsUsed()
    {
        Assert.IsTrue(
            RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Software Development\RequestTypes\ncm.png"),
            "A genuine override path must be taken.");
        Assert.IsTrue(
            RequestImagePathPolicy.IsUsableResolvedPath("Assets/RequestTypes/pickup.png"),
            "A genuine relative catalog path must be taken.");
    }

    [TestMethod]
    public void TheServicesOwnPlaceholderIsNotAnImage()
    {
        // The service answers with these when nothing is configured, which is not the same as an image.
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestItemDefaultPath),
            "The request-item placeholder must not be taken as a resolved image.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestCategoryDefaultPath),
            "The request-category placeholder must not be taken as a resolved image.");
    }

    [TestMethod]
    public void ThePlaceholderIsRecognisedWhateverTheSeparatorsOrCasing()
    {
        // The service hands back backslashes for its own defaults; a resolved catalog path uses forward slashes.
        // A comparison that missed that would let exactly the bug back in.
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("Assets\\Placeholders\\default-no-image.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("assets/placeholders/DEFAULT-NO-IMAGE.PNG"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("./Assets/Placeholders/default-no-image.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder("  Assets/Placeholders/default-no-image.png  "));
    }

    /// <summary>
    /// Every scope answers "nothing configured" with <b>one</b> file, so no surface has to know which scope it is
    /// looking at to know whether it is being shown a placeholder. The per-scope artwork that used to answer here
    /// has been retired: a person now learns one picture as "no image".
    /// </summary>
    [TestMethod]
    public void EveryScopeAnswersWithTheSameNoImagePlaceholder()
    {
        Assert.AreEqual(ImagePicturePolicy.NoImagePath, ImageLocationDefaults.NoImagePath);
        Assert.AreEqual(ImagePicturePolicy.NoImagePath, ImageLocationDefaults.WorkCenterDefaultPath);
        Assert.AreEqual(ImagePicturePolicy.NoImagePath, ImageLocationDefaults.RequestItemDefaultPath);
        Assert.AreEqual(ImagePicturePolicy.NoImagePath, ImageLocationDefaults.RequestCategoryDefaultPath);
    }

    [TestMethod]
    public void NothingAtAllIsNotUsableEither()
    {
        // Null and blank mean the resolver had nothing; the row keeps its own image in both cases.
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath(null));
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath(string.Empty));
        Assert.IsFalse(RequestImagePathPolicy.IsUsableResolvedPath("   "));
    }

    [TestMethod]
    public void AnEmptyPathIsNotAPlaceholder()
    {
        // Guard against the test above passing for the wrong reason: "nothing" and "the placeholder" are
        // different answers and are reported differently.
        Assert.IsFalse(RequestImagePathPolicy.IsServicePlaceholder(null));
        Assert.IsFalse(RequestImagePathPolicy.IsServicePlaceholder("   "));
    }

    /// <summary>
    /// The picture resolves Item → Category family → the existing placeholder (FR-009), and only the *real*
    /// answers are taken. Both new scopes' placeholders are the service's "nothing configured" answer, so
    /// neither may be mistaken for a picture.
    /// </summary>
    [TestMethod]
    public void TheItemAndCategoryPlaceholdersAreNotImages()
    {
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestItemDefaultPath),
            "The request-item placeholder must not be taken as a resolved image.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImageLocationDefaults.RequestCategoryDefaultPath),
            "The request-category placeholder must not be taken as a resolved image.");

        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder(ImageLocationDefaults.RequestItemDefaultPath));
        Assert.IsTrue(RequestImagePathPolicy.IsServicePlaceholder(ImageLocationDefaults.RequestCategoryDefaultPath));
    }

    /// <summary>
    /// A genuine answer from either hop of the cascade — the Item's own picture, or the Category family's —
    /// is taken, as far as the path itself can say.
    /// </summary>
    [TestMethod]
    public void EitherHopOfTheItemCascadeIsTakenWhenItResolvesARealImage()
    {
        Assert.IsTrue(RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Shared\RequestItems\pickup-coil.png"));
        Assert.IsTrue(RequestImagePathPolicy.IsUsableResolvedPath(@"X:\Shared\RequestCategories\Pickup.png"));
    }

    /// <summary>
    /// The whole question the card asks, and the defect it exists for: the six <c>Assets/RequestTypes/*.png</c>
    /// ship as 68-byte single-white-pixel stand-ins, so a seeded override naming one of them is a real path to a
    /// file with no picture in it. Taking it blanked the card's tile the moment the image service was initialized.
    /// </summary>
    [TestMethod]
    public void APathThatCarriesNoPictureIsNotTaken()
    {
        ImageHeaderFixtures.WriteShippedSinglePixelPng(_standInPath);
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPicture(_standInPath),
            "A file holding nothing but a single white pixel must not be taken as a picture.");

        ImageHeaderFixtures.WritePng(_picturePath, 96, 96);
        Assert.IsTrue(
            RequestImagePathPolicy.IsUsableResolvedPicture(_picturePath),
            "A real picture must be taken.");

        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPicture(Path.Combine(_directory, "absent.png")),
            "A path with no file behind it must not be taken as a picture.");

        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPicture(ImageLocationDefaults.RequestItemDefaultPath),
            "The resolver's own placeholder must be refused before the file is even considered.");
    }

    /// <summary>
    /// The same rule stated as the caller's own decision, for the picture-less file: a row that already draws a
    /// picture keeps it.
    /// </summary>
    [TestMethod]
    public void APicturelessFileNeverReplacesAnImageTheRequestAlreadyResolves()
    {
        var alreadyResolved = "Assets/pickup_wip.png";
        ImageHeaderFixtures.WriteShippedSinglePixelPng(_standInPath);

        var chosen = RequestImagePathPolicy.IsUsableResolvedPicture(_standInPath)
            ? _standInPath
            : alreadyResolved;

        Assert.AreEqual(
            alreadyResolved,
            chosen,
            "A seeded stand-in replaced the picture the row already draws, which is what this rule exists to stop.");
    }

    /// <summary>
    /// FR-009/FR-021 stated as the caller's own decision: a request that already resolves a picture keeps it
    /// when the service answers "nothing configured", whatever hop that answer came from.
    /// </summary>
    [TestMethod]
    public void ANothingConfiguredAnswerNeverReplacesAnImageTheRequestAlreadyResolves()
    {
        var alreadyResolved = @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images\request_item\pickup-coil.png";

        foreach (var nothingConfigured in new[]
        {
            ImageLocationDefaults.RequestItemDefaultPath,
            ImageLocationDefaults.RequestCategoryDefaultPath,
            string.Empty,
            null,
        })
        {
            var chosen = RequestImagePathPolicy.IsUsableResolvedPath(nothingConfigured)
                ? nothingConfigured
                : alreadyResolved;

            Assert.AreEqual(
                alreadyResolved,
                chosen,
                $"'{nothingConfigured}' replaced the image the request already resolves, which FR-009/FR-021 forbid.");
        }
    }

    // ── A part drawn without the placeholder path fails here (US1, FR-014, SC-002) ───────────────────────────────

    /// <summary>The properties a surface binds when it draws a part, in the spellings this application uses.</summary>
    private static readonly string[] s_partPictureProperties =
    [
        "ImagePath",
        "PartPicturePath",
        "PicturePath",
        "ResolvedImagePath",
        "ResolvedPartImagePath",
        "EffectiveImagePath",
        "CurrentPicturePath",
    ];

    /// <summary>
    /// The picture the surfaces draw when they have nothing usable of their own is never blank.
    /// </summary>
    /// <remarks>
    /// The other half of the scan below: a surface may reach the placeholder only through a converter, and the
    /// converter only ever answers with this one path. A blank or missing path here would put the blank space back
    /// that FR-014 forbids, behind a converter everything still goes through.
    /// </remarks>
    [TestMethod]
    public void TheOnePlaceholderIsNeverBlank()
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ImagePicturePolicy.NoImagePath),
            "There is no blank space where a part's picture belongs: the fallback is a file.");
        Assert.IsFalse(
            RequestImagePathPolicy.IsUsableResolvedPath(ImagePicturePolicy.NoImagePath),
            "The placeholder is the answer to 'there is nothing to draw', not a picture a surface may mistake for a part's own.");
    }

    /// <summary>
    /// Every surface that draws a part goes through the converter that substitutes the shared placeholder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A scan rather than a per-surface case, because the failure it catches is a <em>new</em> surface: a binding
    /// written as <c>Source="{x:Bind ImagePath}"</c> draws nothing at all when the part has no picture, and no
    /// existing per-surface test notices a file it was never told about.
    /// </para>
    /// <para>
    /// The converter is not named here, it is read from the markup that declares it and its own source is read to
    /// see where it answers. That is what makes the dunnage list's own converter acceptable beside the app-wide
    /// one: both end at the one shared placeholder, and FR-033 keeps dunnage arrangement as it is. What the scan
    /// refuses is a part picture bound with no converter, with a key nothing declares, or through a converter that
    /// answers with something other than the shared placeholder — the ways a part with no picture ends as a blank
    /// space or as a stand-in that is not the part's own picture.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void EverySurfaceThatDrawsAPart_PassesItThroughThePlaceholderConverter()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var markupFiles = EnumerateMarkup(root).ToArray();
        Assert.IsTrue(markupFiles.Length > 0, "The scan found no markup to read, so it would pass without proving anything.");

        var declaredConverters = markupFiles
            .SelectMany(file => XDocument.Load(file).Descendants())
            .Select(element => (
                Type: element.Name.LocalName,
                Key: element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "Key")?.Value))
            .Where(declared => declared.Key is not null
                && declared.Type.EndsWith("Converter", StringComparison.Ordinal))
            .ToList();

        // A converter counts when its own source answers through PictureSource, which is the one place a picture
        // path becomes a bitmap and so the one place "there is nothing to draw" becomes the shared placeholder.
        var placeholderConverters = EnumerateSource(root)
            .Where(file => File.ReadAllText(file).Contains("PictureSource.", StringComparison.Ordinal))
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(
            placeholderConverters.Contains("ResolvedImagePathToSourceConverter"),
            "The converter that substitutes the placeholder has no source that answers with it, so every picture binding below would be reported.");

        var offenders = new List<string>();

        foreach (var file in markupFiles)
        {
            var relativePath = Path.GetRelativePath(root, file);

            foreach (var attribute in XDocument.Load(file)
                .Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => attribute.Name.LocalName is "Source" or "ImageSource"))
            {
                var binding = attribute.Value;
                if (!binding.Contains("{Binding", StringComparison.Ordinal)
                    && !binding.Contains("{x:Bind", StringComparison.Ordinal))
                {
                    continue;
                }

                var property = s_partPictureProperties
                    .FirstOrDefault(name => Regex.IsMatch(binding, $@"\b{Regex.Escape(name)}\b"));

                if (property is null)
                {
                    continue;
                }

                var key = Regex.Match(binding, @"Converter=\{StaticResource\s+(?<key>[A-Za-z0-9_]+)\}").Groups["key"].Value;

                if (key.Length == 0)
                {
                    offenders.Add(
                        $"{relativePath}: '{property}' is drawn with no converter, so a part with no picture draws nothing at all (FR-014).");
                    continue;
                }

                var converterType = declaredConverters
                    .Where(declared => string.Equals(declared.Key, key, StringComparison.Ordinal))
                    .Select(declared => declared.Type)
                    .FirstOrDefault();

                if (converterType is null)
                {
                    offenders.Add(
                        $"{relativePath}: '{property}' goes through '{key}', which no markup declares, so the placeholder never applies (FR-014).");
                }
                else if (placeholderConverters.Contains(converterType) is false)
                {
                    offenders.Add(
                        $"{relativePath}: '{property}' goes through '{key}', a {converterType} that never answers with the shared placeholder (FR-014).");
                }
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "Every surface that draws a part must fall back to the one shared placeholder:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Every markup file the scan reads: source markup, not build output or design notes.</summary>
    private static IEnumerable<string> EnumerateMarkup(string root)
    {
        string[] excluded =
        [
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        ];

        foreach (var file in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
        {
            if (excluded.Any(folder => file.Contains(folder, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>Every production C# file the scan reads, so a converter's own behaviour can be looked at.</summary>
    private static IEnumerable<string> EnumerateSource(string root)
    {
        string[] excluded =
        [
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        ];

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (excluded.Any(folder => file.Contains(folder, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            yield return file;
        }
    }
}
