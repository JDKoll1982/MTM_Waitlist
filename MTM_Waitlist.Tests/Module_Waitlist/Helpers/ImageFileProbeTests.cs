using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The card's picture rule reads the file, not just its path: the six <c>Assets/RequestTypes/*.png</c> ship as
/// 68-byte single-white-pixel stand-ins, and a configured override pointing at one of them is a real path to a
/// file with no picture in it. Taking it blanked the card's tile the moment the image service was initialized.
/// </summary>
[TestClass]
public sealed class ImageFileProbeTests
{
    private string _directory = string.Empty;
    private string _filePath = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = ImageHeaderFixtures.CreateTemporaryDirectory();
        _filePath = Path.Combine(_directory, "picture.png");
    }

    [TestCleanup]
    public void TestCleanup() => ImageHeaderFixtures.DeleteTemporaryDirectory(_directory);

    /// <summary>
    /// The artifact this rule exists for: the shipped 68-byte stand-in reports one pixel by one pixel.
    /// </summary>
    [TestMethod]
    public void TheShippedSinglePixelStandInCarriesNoPicture()
    {
        ImageHeaderFixtures.WriteShippedSinglePixelPng(_filePath);

        Assert.IsFalse(
            ImageFileProbe.CarriesPicture(_filePath),
            "A single white pixel is not artwork, and must not be drawn where the card has a real picture.");
    }

    /// <summary>
    /// The floor is the application's own, not the file's byte count: a padded single-pixel PNG is refused on its
    /// dimensions alone, and a small-but-real picture is refused for being smaller than the app accepts.
    /// </summary>
    [TestMethod]
    public void APictureBelowTheApplicationsMinimumIsNotUsable()
    {
        ImageHeaderFixtures.WritePng(_filePath, 1, 1);
        Assert.IsFalse(ImageFileProbe.CarriesPicture(_filePath), "A one-pixel picture must be refused.");

        ImageHeaderFixtures.WritePng(_filePath, ImageFileProbe.MinimumPixels - 1, ImageFileProbe.MinimumPixels - 1);
        Assert.IsFalse(
            ImageFileProbe.CarriesPicture(_filePath),
            "A picture smaller than the smallest the app accepts must be refused.");
    }

    [TestMethod]
    public void APictureAtOrAboveTheMinimumIsUsable()
    {
        ImageHeaderFixtures.WritePng(_filePath, ImageFileProbe.MinimumPixels, ImageFileProbe.MinimumPixels);
        Assert.IsTrue(ImageFileProbe.CarriesPicture(_filePath), "The smallest accepted picture is a picture.");

        ImageHeaderFixtures.WritePng(_filePath, 96, 96);
        Assert.IsTrue(ImageFileProbe.CarriesPicture(_filePath), "The card's own tile size is a picture.");
    }

    /// <summary>
    /// A JPEG carries its dimensions in the frame header, which sits behind its metadata segments — including an
    /// EXIF block large enough to matter.
    /// </summary>
    [TestMethod]
    public void AJpegsDimensionsAreFoundBehindItsMetadata()
    {
        ImageHeaderFixtures.WriteJpegHeader(_filePath, 96, 96);
        Assert.IsTrue(ImageFileProbe.CarriesPicture(_filePath), "A JPEG's frame header carries its dimensions.");

        var withMetadataPath = Path.Combine(_directory, "with-exif.jpg");
        ImageHeaderFixtures.WriteJpegHeader(withMetadataPath, 96, 96, metadataBytes: 20_000);
        Assert.IsTrue(
            ImageFileProbe.CarriesPicture(withMetadataPath),
            "A frame header must still be found behind a large metadata segment.");
    }

    [TestMethod]
    public void AGifAndABmpCarryTheirDimensionsInTheirHeads()
    {
        var gifPath = Path.Combine(_directory, "picture.gif");
        ImageHeaderFixtures.WriteGifHeader(gifPath, 96, 96);
        Assert.IsTrue(ImageFileProbe.CarriesPicture(gifPath), "A GIF states its size in its header.");

        var bmpPath = Path.Combine(_directory, "picture.bmp");
        ImageHeaderFixtures.WriteBmpHeader(bmpPath, 96, 96);
        Assert.IsTrue(ImageFileProbe.CarriesPicture(bmpPath), "A BMP states its size in its header.");
    }

    /// <summary>
    /// The answers that are not pictures: nothing named at all, a path with no file behind it, and a file that is
    /// no kind of picture.
    /// </summary>
    [TestMethod]
    public void NothingThereIsNotAPicture()
    {
        Assert.IsFalse(ImageFileProbe.CarriesPicture(null), "No path is no picture.");
        Assert.IsFalse(ImageFileProbe.CarriesPicture(string.Empty), "A blank path is no picture.");
        Assert.IsFalse(ImageFileProbe.CarriesPicture("   "), "A whitespace path is no picture.");

        Assert.IsFalse(
            ImageFileProbe.CarriesPicture(Path.Combine(_directory, "absent.png")),
            "A path with no file behind it is no picture.");

        ImageHeaderFixtures.WriteTextFile(_filePath);
        Assert.IsFalse(ImageFileProbe.CarriesPicture(_filePath), "A file that is not a picture is no picture.");
    }

    /// <summary>
    /// An application-relative path is resolved the way the resolver and the card's converter resolve it: against
    /// the application directory. The fixture is written into that directory so the answer is about the rule and
    /// not about where the test happens to run.
    /// </summary>
    [TestMethod]
    public void AnApplicationRelativePathIsResolvedAgainstTheApplicationDirectory()
    {
        var relativeDirectory = Path.Combine(AppContext.BaseDirectory, "image-probe-tests", Guid.NewGuid().ToString("N"));
        var relativePath = Path.Combine("image-probe-tests", Path.GetFileName(relativeDirectory), "picture.png");

        try
        {
            ImageHeaderFixtures.WritePng(Path.Combine(AppContext.BaseDirectory, relativePath), 96, 96);

            Assert.IsTrue(
                ImageFileProbe.CarriesPicture(relativePath),
                "A relative path is taken against the application directory.");

            Assert.IsTrue(
                ImageFileProbe.CarriesPicture(relativePath.Replace('/', Path.DirectorySeparatorChar)),
                "A relative path is understood whatever separator it uses.");
        }
        finally
        {
            ImageHeaderFixtures.DeleteTemporaryDirectory(relativeDirectory);
        }
    }

    /// <summary>
    /// The whole question the surfaces ask — <see cref="ImageFileProbe.IsUsablePicture"/> — is stricter than
    /// <see cref="ImageFileProbe.CarriesPicture"/>: the application's image screens only accept square pictures, so
    /// a picture of the wrong shape is refused and the surface draws the no-image placeholder instead.
    /// </summary>
    [TestMethod]
    public void APictureOfTheWrongShapeIsNotUsable()
    {
        ImageHeaderFixtures.WritePng(_filePath, 96, 96);
        Assert.IsTrue(ImageFileProbe.IsUsablePicture(_filePath), "A square picture at the accepted size is usable.");

        ImageHeaderFixtures.WritePng(_filePath, 96, 48);
        Assert.IsFalse(
            ImageFileProbe.IsUsablePicture(_filePath),
            "A picture that is not square is refused, however many pixels it carries.");
        Assert.IsTrue(
            ImageFileProbe.CarriesPicture(_filePath),
            "The size-only question still answers for it, because being the wrong shape is not being too small.");

        ImageHeaderFixtures.WritePng(_filePath, 48, 96);
        Assert.IsFalse(ImageFileProbe.IsUsablePicture(_filePath), "The shape rule is not one-directional.");
    }

    /// <summary>
    /// The size floor and the shape rule are one answer: the floor itself is accepted, and anything under it is
    /// refused whatever its shape.
    /// </summary>
    [TestMethod]
    public void TheUsablePictureFloorIsTheApplicationsOwn()
    {
        ImageHeaderFixtures.WritePng(_filePath, ImageFileProbe.MinimumPixels - 1, ImageFileProbe.MinimumPixels - 1);
        Assert.IsFalse(ImageFileProbe.IsUsablePicture(_filePath), "A picture under the floor is refused.");

        ImageHeaderFixtures.WritePng(_filePath, ImageFileProbe.MinimumPixels, ImageFileProbe.MinimumPixels);
        Assert.IsTrue(ImageFileProbe.IsUsablePicture(_filePath), "The smallest accepted picture is usable.");
    }

    /// <summary>
    /// The answers that are not pictures, asked of the whole rule: nothing named, a path with no file behind it,
    /// and a file that is no kind of picture.
    /// </summary>
    [TestMethod]
    public void NothingThereIsNotAUsablePicture()
    {
        Assert.IsFalse(ImageFileProbe.IsUsablePicture(null), "No path is no picture.");
        Assert.IsFalse(ImageFileProbe.IsUsablePicture(string.Empty), "A blank path is no picture.");
        Assert.IsFalse(ImageFileProbe.IsUsablePicture("   "), "A whitespace path is no picture.");
        Assert.IsFalse(
            ImageFileProbe.IsUsablePicture(Path.Combine(_directory, "absent.png")),
            "A path with no file behind it is no picture.");

        ImageHeaderFixtures.WriteTextFile(_filePath);
        Assert.IsFalse(ImageFileProbe.IsUsablePicture(_filePath), "A file that is not a picture is no picture.");
    }
}
