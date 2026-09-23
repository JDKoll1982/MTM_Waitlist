using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Tests.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// What makes one recorded part picture resolve for every computer: the value is stored relative to the configured
/// folder, never as a folder of its own (US6, FR-009, FR-010).
/// </summary>
/// <remarks>
/// The two machines here are two folders on this machine, which is enough to prove the point: the recorded value is
/// read against whichever root it is resolved with, and neither root is written into it. A value that named the
/// folder it was recorded under would resolve for the machine that wrote it and for no other, which is the defect
/// the relative-path rule exists to prevent.
/// </remarks>
[TestClass]
public sealed class PartPictureStoragePathPortabilityTests
{
    private const string PartNumber = "MMC-12345";

    private string _workingDirectory = string.Empty;
    private string _firstRoot = string.Empty;
    private string _secondRoot = string.Empty;
    private string _firstCacheRoot = string.Empty;
    private string _secondCacheRoot = string.Empty;
    private string? _cacheRootBeforeTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _workingDirectory = ImageHeaderFixtures.CreateTemporaryDirectory();
        _firstRoot = Path.Combine(_workingDirectory, "machine-one", "images");
        _secondRoot = Path.Combine(_workingDirectory, "machine-two", "images");
        _firstCacheRoot = Path.Combine(_workingDirectory, "machine-one", "cache");
        _secondCacheRoot = Path.Combine(_workingDirectory, "machine-two", "cache");

        // ImageCachePaths holds one configured root for the process, so it is put back exactly as it was found.
        _cacheRootBeforeTest = ImageCachePaths.LocalCacheRoot;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        ImageCachePaths.SetCacheRoot(_cacheRootBeforeTest);

        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void ARecordedPartPictureNamesNoFolderOfItsOwn()
    {
        var recorded = PartPictureLayout.RelativePathFor(PartPictureLayout.VisualPartScope, PartNumber, ".png");

        Assert.IsNotNull(recorded);
        Assert.IsFalse(Path.IsPathRooted(recorded), "A rooted value can only be read on the machine that wrote it.");
        Assert.IsFalse(
            recorded.Contains(':', StringComparison.Ordinal),
            "A drive letter in a recorded value is a mapping that some other machine will not have.");
        Assert.IsFalse(
            AppStoragePaths.CollectionFolderNames.Any(folder =>
                Path.IsPathRooted(recorded) && recorded.StartsWith(folder, StringComparison.OrdinalIgnoreCase)),
            "The recorded value is relative to the configured folder, not to a place inside it.");
    }

    [TestMethod]
    public void OneRecordedValueResolvesUnderEachMachinesOwnConfiguredFolder()
    {
        var recorded = PartPictureLayout.RelativePathFor(PartPictureLayout.VisualPartScope, PartNumber, ".png")!;

        var onFirstMachine = AppStoragePaths.ResolvePicturePath(_firstRoot, recorded);
        var onSecondMachine = AppStoragePaths.ResolvePicturePath(_secondRoot, recorded);

        Assert.IsNotNull(onFirstMachine);
        Assert.IsNotNull(onSecondMachine);
        Assert.AreNotEqual(
            onFirstMachine,
            onSecondMachine,
            "The same record resolves to a file under each machine's own folder, which is the whole point of storing it relative.");
        StringAssert.StartsWith(onFirstMachine, _firstRoot);
        StringAssert.StartsWith(onSecondMachine, _secondRoot);
        Assert.IsFalse(onFirstMachine!.Contains(_secondRoot, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(onSecondMachine!.Contains(_firstRoot, StringComparison.OrdinalIgnoreCase));

        // The part of the path below the configured folder is identical on both, so both find the same picture.
        Assert.AreEqual(
            Path.GetRelativePath(_firstRoot, onFirstMachine),
            Path.GetRelativePath(_secondRoot, onSecondMachine));
    }

    [TestMethod]
    public void EachMachineDrawsItsOwnCopyOfTheSameRecordedPicture()
    {
        var recorded = PartPictureLayout.RelativePathFor(PartPictureLayout.VisualPartScope, PartNumber, ".png")!;

        var copyOnFirstMachine = WriteCachedCopy(_firstCacheRoot, recorded);
        var copyOnSecondMachine = WriteCachedCopy(_secondCacheRoot, recorded);

        ImageCachePaths.SetCacheRoot(_firstCacheRoot);
        var drawnOnFirstMachine = PartPictureResolver.ResolveUsableFile(PartPictureLayout.VisualPartScope, recorded, _firstRoot);

        ImageCachePaths.SetCacheRoot(_secondCacheRoot);
        var drawnOnSecondMachine = PartPictureResolver.ResolveUsableFile(PartPictureLayout.VisualPartScope, recorded, _secondRoot);

        Assert.AreEqual(
            Path.GetFullPath(copyOnFirstMachine),
            Path.GetFullPath(drawnOnFirstMachine!),
            "A machine draws the copy it holds rather than reaching for the share.");
        Assert.AreEqual(Path.GetFullPath(copyOnSecondMachine), Path.GetFullPath(drawnOnSecondMachine!));
        Assert.AreNotEqual(drawnOnFirstMachine, drawnOnSecondMachine);
    }

    [TestMethod]
    public void AMachineWithNoCopyFallsBackToItsOwnShareFile()
    {
        var recorded = PartPictureLayout.RelativePathFor(PartPictureLayout.VisualPartScope, PartNumber, ".png")!;
        var sharedPath = Path.Combine(_secondRoot, AppStoragePaths.NormalizeSeparators(recorded));
        Directory.CreateDirectory(Path.GetDirectoryName(sharedPath)!);
        ImageHeaderFixtures.WritePng(sharedPath, 64, 64);

        ImageCachePaths.SetCacheRoot(Path.Combine(_workingDirectory, "empty-cache"));

        var drawn = PartPictureResolver.ResolveUsableFile(PartPictureLayout.VisualPartScope, recorded, _secondRoot);

        Assert.AreEqual(
            Path.GetFullPath(sharedPath),
            Path.GetFullPath(drawn!),
            "With no copy of its own, the machine reads the picture from the folder it is configured with.");
    }

    /// <summary>Writes this machine's copy of a recorded picture, where the cache keeps it.</summary>
    private static string WriteCachedCopy(string cacheRoot, string recordedPath)
    {
        ImageCachePaths.SetCacheRoot(cacheRoot);

        var collectionFolder = PartPictureLayout.CollectionFolderFor(PartPictureLayout.VisualPartScope)!;
        var normalized = AppStoragePaths.NormalizeSeparators(recordedPath);
        var belowCollection = normalized.StartsWith(collectionFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? normalized[(collectionFolder.Length + 1)..]
            : normalized;

        var copy = ImageCachePaths.CachePathFor(
            ImageCachePaths.PartCollectionCacheFolder(collectionFolder),
            belowCollection);

        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        ImageHeaderFixtures.WritePng(copy, 64, 64);
        return copy;
    }
}
