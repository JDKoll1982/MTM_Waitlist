using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The lugger pictures: which file a lugger's card looks for, and what happens when there is none.
/// </summary>
/// <remarks>
/// The naming rule is the whole contract with the artwork on the share, so it is pinned here rather than left to be
/// discovered by a card that draws the wrong lugger's picture.
/// </remarks>
[TestClass]
public sealed class LuggerImageResolverTests
{
    private string _folder = string.Empty;

    [TestInitialize]
    public void NewFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "lugger-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void DeleteFolder()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [TestMethod]
    public void TheDefaultFolder_IsTheAgreedShareFolder()
    {
        Assert.AreEqual(
            @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images\Waitlist\lugger",
            MTM_Waitlist.Module_Settings.Models.ConfigSettingKeys.ImageStorageLuggerFolderPathDefault,
            "The shipped lugger folder is the share folder the artwork was agreed to live in.");
    }

    [TestMethod]
    public void TheFileName_IsTheLuggersPlaceInTheArrangement_TwoDigitsWide()
    {
        Assert.AreEqual("Lugger01", LuggerImageResolver.FileNameStem(1), "The first lugger is Lugger01, not Lugger1.");
        Assert.AreEqual("Lugger09", LuggerImageResolver.FileNameStem(9));
        Assert.AreEqual("Lugger10", LuggerImageResolver.FileNameStem(10), "The ten is not padded a third digit.");
        Assert.AreEqual("Lugger99", LuggerImageResolver.FileNameStem(99));
    }

    [TestMethod]
    public void ThereIsNoLuggerPastTheNinetyNinth_AndNoneBeforeTheFirst()
    {
        File.WriteAllBytes(Path.Combine(_folder, "Lugger01.png"), [1]);

        Assert.IsNull(LuggerImageResolver.ResolveInFolder(_folder, 0), "A slot before the first has no picture.");
        Assert.IsNull(LuggerImageResolver.ResolveInFolder(_folder, 100), "The arrangement holds at most 99 luggers.");
        Assert.IsNotNull(LuggerImageResolver.ResolveInFolder(_folder, 1), "The first slot still resolves.");
    }

    [TestMethod]
    public void EverySupportedExtension_IsFound()
    {
        foreach (var extension in LuggerImageResolver.SupportedExtensions)
        {
            var folder = Path.Combine(_folder, extension.TrimStart('.'));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "Lugger07" + extension), [1]);

            var resolved = LuggerImageResolver.ResolveInFolder(folder, 7);

            Assert.IsNotNull(resolved, $"A Lugger07{extension} file was not found.");
            Assert.AreEqual("Lugger07" + extension, Path.GetFileName(resolved), ignoreCase: true);
        }
    }

    [TestMethod]
    public void PngWins_WhenTwoFilesClaimTheSameSlot()
    {
        File.WriteAllBytes(Path.Combine(_folder, "Lugger03.jpg"), [1]);
        File.WriteAllBytes(Path.Combine(_folder, "Lugger03.png"), [1]);

        var resolved = LuggerImageResolver.ResolveInFolder(_folder, 3);

        Assert.IsNotNull(resolved);
        Assert.AreEqual(".png", Path.GetExtension(resolved), "The extension order decides, so a slot is never ambiguous.");
    }

    [TestMethod]
    public void AMissingFileOrFolder_ResolvesToNoPicture()
    {
        Assert.IsNull(
            LuggerImageResolver.ResolveInFolder(_folder, 4),
            "An empty folder gives no picture, which the card draws as the no-picture image.");

        Assert.IsNull(
            LuggerImageResolver.ResolveInFolder(Path.Combine(_folder, "not-there"), 4),
            "An unreachable folder gives no picture rather than a failure.");

        Assert.IsNull(LuggerImageResolver.ResolveInFolder(null, 4));
        Assert.IsNull(LuggerImageResolver.ResolveInFolder("   ", 4));
    }
}
