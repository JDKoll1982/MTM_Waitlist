using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The picture the plant draws where a surface has none of its own.
/// </summary>
/// <remarks>
/// The plant's own artwork is a setting, and every surface reaches it through
/// <see cref="ImagePicturePolicy.ResolveNoImageFile()"/>. Two things have to hold: the plant's picture wins while
/// it is there, and the shipped artwork is drawn when it is not — a share that is briefly unreachable must not
/// leave a card blank. The picture is taken by existence rather than by the picture rule, because the machine
/// frames that draw it are not all square.
/// </remarks>
[TestClass]
public sealed class NoImageArtworkTests
{
    /// <summary>The shipped artwork, written into this run's output folder so both branches can be exercised.</summary>
    private string _shippedPath = string.Empty;

    private bool _shippedPathWasWritten;

    [TestInitialize]
    public void TestInitialize()
    {
        _shippedPath = Path.Combine(
            AppContext.BaseDirectory,
            ImagePicturePolicy.NoImagePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(_shippedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_shippedPath)!);
            File.WriteAllBytes(_shippedPath, [0x89, 0x50, 0x4E, 0x47]);
            _shippedPathWasWritten = true;
        }

        ImagePicturePolicy.SetNoImageOverride(null);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImagePicturePolicy.SetNoImageOverride(null);

        if (_shippedPathWasWritten && File.Exists(_shippedPath))
        {
            File.Delete(_shippedPath);
        }
    }

    /// <summary>
    /// The picture the plant chose is what the placeholder draws, and its shape is not policed: the artwork is read
    /// by existence, so a wide banner is drawn by the frames that draw it rather than replaced by the shipped one.
    /// </summary>
    [TestMethod]
    public void Service_NoImageArtworkTests_ThePlantsOwnPictureIsDrawnWhileItIsThere()
    {
        var directory = ImageHeaderFixtures.CreateTemporaryDirectory();

        try
        {
            var chosen = Path.Combine(directory, "no-picture-wide.png");
            File.WriteAllBytes(chosen, [0x89, 0x50, 0x4E, 0x47]);

            ImagePicturePolicy.SetNoImageOverride(chosen);

            Assert.AreEqual(
                chosen,
                ImagePicturePolicy.ResolveNoImageFile(),
                "the plant's own artwork is what every surface must draw");
        }
        finally
        {
            ImageHeaderFixtures.DeleteTemporaryDirectory(directory);
        }
    }

    /// <summary>
    /// A picture that has gone — a share that is down, a file somebody renamed — falls back to the shipped artwork
    /// rather than leaving the surface with nothing to draw.
    /// </summary>
    [TestMethod]
    public void Service_NoImageArtworkTests_AMissingPictureFallsBackToTheShippedArtwork()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"mtm-missing-{Guid.NewGuid():N}.png");

        Assert.AreEqual(
            _shippedPath,
            ImagePicturePolicy.ResolveNoImageFile(missing),
            "a missing plant picture must fall back to the artwork the application ships with");
    }

    /// <summary>
    /// Blank means the shipped artwork: the box being emptied is an answer, not a failure.
    /// </summary>
    [TestMethod]
    public void Service_NoImageArtworkTests_ABlankPictureIsTheShippedArtwork()
    {
        ImagePicturePolicy.SetNoImageOverride("   ");

        Assert.AreEqual(string.Empty, ImagePicturePolicy.NoImageOverridePath, "blank is adopted as no choice at all");
        Assert.AreEqual(
            _shippedPath,
            ImagePicturePolicy.ResolveNoImageFile(),
            "with nothing chosen, every surface draws the artwork the application ships with");
    }

    /// <summary>
    /// The placeholder's own path is recognised however it is spelled, so a view model that hands it back as if it
    /// were a picture's still reaches the plant's artwork instead of the shipped file.
    /// </summary>
    [TestMethod]
    public void Service_NoImageArtworkTests_ThePlaceholderPathIsRecognisedInEitherSpelling()
    {
        Assert.IsTrue(ImagePicturePolicy.IsNoImagePath(ImagePicturePolicy.NoImagePath));
        Assert.IsTrue(
            ImagePicturePolicy.IsNoImagePath(ImagePicturePolicy.NoImagePath.Replace('/', '\\')),
            "a backslash spelling is the same picture on this platform");
        Assert.IsTrue(
            ImagePicturePolicy.IsNoImagePath($"  {ImagePicturePolicy.NoImagePackUri}  "),
            "the package URI is the same placeholder, and a padded box is the same path");
        Assert.IsFalse(
            ImagePicturePolicy.IsNoImagePath(@"\\share\pictures\no-picture.png"),
            "a picture the plant chose is not the placeholder, so it is resolved down the ordinary path");
        Assert.IsFalse(ImagePicturePolicy.IsNoImagePath(null));
    }
}
