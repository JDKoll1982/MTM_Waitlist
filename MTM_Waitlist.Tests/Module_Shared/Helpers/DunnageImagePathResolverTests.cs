using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Tests.Module_Shared.Helpers;

[TestClass]
public sealed class DunnageImagePathResolverTests
{
    private string? _tempRoot;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "mtm_dunnage_resolver_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        DunnageImagePathResolver.ConfigureRootFolder(_tempRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        DunnageImagePathResolver.ConfigureRootFolder(null);
        if (_tempRoot is not null && Directory.Exists(_tempRoot))
        {
            try
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Best-effort cleanup of the temp folder.
            }
        }
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_RootFolder_UsesConfiguredValueWhenSet()
    {
        Assert.AreEqual(_tempRoot, DunnageImagePathResolver.RootFolder);
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_GetDisplayPath_ReturnsNullWhenPathIsBlank()
    {
        Assert.IsNull(DunnageImagePathResolver.GetDisplayPath(null));
        Assert.IsNull(DunnageImagePathResolver.GetDisplayPath("   "));
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_GetDisplayPath_ReturnsNullWhenFileDoesNotExist()
    {
        var resolved = DunnageImagePathResolver.GetDisplayPath("Parts/Missing Dunnage.png");
        Assert.IsNull(resolved);
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_GetDisplayPath_ReturnsAbsolutePathWhenRelativeFileExists()
    {
        var partsFolder = Path.Combine(_tempRoot!, "Parts");
        Directory.CreateDirectory(partsFolder);
        var file = Path.Combine(partsFolder, "Steel Rack.png");
        File.WriteAllText(file, "fixture");

        var resolved = DunnageImagePathResolver.GetDisplayPath("Parts/Steel Rack.png");

        Assert.IsNotNull(resolved);
        Assert.IsTrue(File.Exists(resolved));
        Assert.IsTrue(string.Equals(resolved, file, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_GetDisplayPath_NormalizesForwardSlashesToDirectorySeparators()
    {
        var typesFolder = Path.Combine(_tempRoot!, "Types");
        Directory.CreateDirectory(typesFolder);
        var file = Path.Combine(typesFolder, "DunnageType-Boxes.png");
        File.WriteAllText(file, "fixture");

        var resolved = DunnageImagePathResolver.GetDisplayPath("Types/DunnageType-Boxes.png");

        Assert.IsNotNull(resolved);
        Assert.IsTrue(File.Exists(resolved));
    }

    [TestMethod]
    public void Service_DunnageImagePathResolverTests_GetDisplayPath_RootedPathIsReturnedAsIsWhenFileExists()
    {
        var file = Path.Combine(_tempRoot!, "already-absolute.png");
        File.WriteAllText(file, "fixture");

        var resolved = DunnageImagePathResolver.GetDisplayPath(file);

        Assert.IsNotNull(resolved);
        Assert.IsTrue(string.Equals(resolved, file, StringComparison.OrdinalIgnoreCase));
    }
}
