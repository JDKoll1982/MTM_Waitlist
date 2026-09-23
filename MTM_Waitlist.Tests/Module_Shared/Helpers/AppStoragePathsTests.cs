using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Shared.Helpers;

/// <summary>
/// The two locations the application reads outside its own folder, stated against the site's own share so a silent
/// edit to either is a test failure rather than a support call.
/// </summary>
[TestClass]
public sealed class AppStoragePathsTests
{
    /// <summary>
    /// The share is named rather than reached through the <c>X:</c> drive letter that maps to it on this site
    /// (<c>X:</c> is <c>\\mtmanu-fs01\Expo Drive</c>): a drive letter is only meaningful on a machine that has the
    /// mapping, and every machine reads these.
    /// </summary>
    [TestMethod]
    public void TheDefaults_NameTheShareRatherThanADriveLetter()
    {
        Assert.AreEqual(
            @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images",
            AppStoragePaths.ImagesRootDefault);

        Assert.AreEqual(
            @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\Keys - DO NOT EDIT FILES\MTM Waitlist Application",
            AppStoragePaths.KeysFolderDefault);

        Assert.IsTrue(
            AppStoragePaths.ImagesRootDefault.StartsWith(@"\\", StringComparison.Ordinal),
            "The picture root must be a share name, not a drive letter.");
        Assert.IsTrue(
            AppStoragePaths.KeysFolderDefault.StartsWith(@"\\", StringComparison.Ordinal),
            "The key-files folder must be a share name, not a drive letter.");
    }

    /// <summary>
    /// A picture is stored relative to the root, which is what lets the root be changed in Settings without
    /// orphaning a row and what makes two machines agree about where the file is.
    /// </summary>
    /// <remarks>
    /// Both recorded layouts are asserted here, because the reader has to answer for both: 008-part-pictures
    /// re-laid the share out under one folder per collection, and a value written before that move names the old
    /// place. The two resolve to the same file, which is what makes the move safe to interrupt.
    /// </remarks>
    [TestMethod]
    public void AStoredPicturePathIsJoinedToTheRoot()
    {
        Assert.AreEqual(
            Path.Combine(AppStoragePaths.ImagesRootDefault, "Waitlist", "request_item", "pickup-coil.png"),
            AppStoragePaths.ResolvePicturePath(
                AppStoragePaths.ImagesRootDefault,
                @"request_item\pickup-coil.png"),
            "A value in the pre-move layout resolves under the collection folder it belongs to.");

        Assert.AreEqual(
            Path.Combine(AppStoragePaths.ImagesRootDefault, "Waitlist", "request_item", "pickup-coil.png"),
            AppStoragePaths.ResolvePicturePath(
                AppStoragePaths.ImagesRootDefault,
                "Waitlist/request_item/pickup-coil.png"),
            "A value already in the move's layout is taken as it stands.");

        Assert.AreEqual(
            Path.Combine(AppStoragePaths.ImagesRootDefault, "Visual", "MMC", "MMC0001000.png"),
            AppStoragePaths.ResolvePicturePath(
                AppStoragePaths.ImagesRootDefault,
                "Visual/MMC/MMC0001000.png"),
            "A part picture names its own collection, so it is never re-prefixed with the application's.");
    }

    /// <summary>
    /// A folder the move does not recognise is left exactly where it is. Guessing would put a picture somewhere no
    /// reader looks.
    /// </summary>
    [TestMethod]
    public void AnUnrecognisedLeadingFolderIsLeftWhereItIs()
    {
        Assert.AreEqual(
            Path.Combine(AppStoragePaths.ImagesRootDefault, "something-else", "pickup-coil.png"),
            AppStoragePaths.ResolvePicturePath(
                AppStoragePaths.ImagesRootDefault,
                "something-else/pickup-coil.png"));
    }

    /// <summary>
    /// A row written before pictures became relative names the file itself, and is taken as it stands: re-basing it
    /// on today's root would move an operator's picture onto a file that is not there.
    /// </summary>
    [TestMethod]
    public void AnAbsoluteStoredPathIsTakenAsItStands()
    {
        const string Absolute = @"X:\Software Development\Live Applications\MTM_Waitlist\Images\request_item_pickup-coil.png";

        Assert.AreEqual(
            Absolute,
            AppStoragePaths.ResolvePicturePath(AppStoragePaths.ImagesRootDefault, Absolute));
    }

    [TestMethod]
    public void NothingToResolveAnswersNothing()
    {
        Assert.IsNull(AppStoragePaths.ResolvePicturePath(AppStoragePaths.ImagesRootDefault, null));
        Assert.IsNull(AppStoragePaths.ResolvePicturePath(AppStoragePaths.ImagesRootDefault, "   "));
        Assert.IsNull(
            AppStoragePaths.ResolvePicturePath(null, @"request_item\pickup-coil.png"),
            "A relative path with no root to join it to cannot be resolved.");
    }

    /// <summary>The key file naming convention is declared once, so a reader cannot invent a second one.</summary>
    [TestMethod]
    public void AKeyIsReadFromItsOwnTextFile()
    {
        Assert.AreEqual("db-connection.txt", AppStoragePaths.KeyFileName("db-connection"));
    }
}
