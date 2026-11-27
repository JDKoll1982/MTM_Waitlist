using System.Text.RegularExpressions;

namespace MTM_Waitlist.Tests.Module_Core;

/// <summary>
/// The development-only store substitution must not be able to reach a published build, and this is what keeps
/// that true.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule this pins.</b> A workstation off the plant network may point the store at its own MySQL server so
/// the application can be run at the weekend. The plant must never be able to do the same by accident, because
/// the plant has no second server and a silent substitution there would let a save land in a copy nobody reads.
/// So the substitution is compiled out of a Release build, and both publish profiles force Release.
/// </para>
/// <para>
/// <b>Why a source-text test.</b> A compile-time constant cannot be observed at runtime: the Release build has no
/// code to test. What can be checked is the wiring that decides it — the build property, the guarded file, and the
/// single seam every call site goes through. That is the same idiom the other guards in this suite use, and it
/// fails the build the moment someone widens the condition or calls the helper directly again.
/// </para>
/// </remarks>
[TestClass]
public sealed class LocalStoreFallbackAuditTests
{
    private const string Guard = "LOCAL_STORE_FALLBACK";

    [TestMethod]
    public void The_Fallback_Is_Defined_Only_Outside_A_Release_Build()
    {
        var props = Read("Directory.Build.props");

        var definition = Regex.Match(
            props,
            @"<PropertyGroup\s+Condition=""([^""]*)""\s*>\s*(?:<!--.*?-->\s*)*<DefineConstants>[^<]*LOCAL_STORE_FALLBACK",
            RegexOptions.Singleline);

        Assert.IsTrue(definition.Success, "Nothing defines LOCAL_STORE_FALLBACK, so a development build cannot reach this machine's store.");
        StringAssert.Contains(
            definition.Groups[1].Value,
            "!= 'Release'",
            "The constant is no longer limited to non-Release builds, so a published build could substitute a server.");
    }

    [TestMethod]
    public void Every_Shipped_Publish_Profile_Forces_Release()
    {
        // The two self-contained profiles are the documented publish path for the desktop application and for the
        // on-host service; both are deployed to plant machines. The win10-*.pubxml files are legacy scaffolding
        // that takes its configuration from the command line, so they are checked for an explicit Debug instead.
        var profiles = Directory
            .EnumerateFiles(RepositoryRoot(), "*.pubxml", SearchOption.AllDirectories)
            .Where(path => path.Contains("PublishProfiles", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.IsTrue(profiles.Length > 0, "No publish profile was found, so nothing guarantees a Release publish.");

        foreach (var profile in profiles)
        {
            var content = File.ReadAllText(profile);
            var name = Path.GetFileName(profile);

            Assert.IsFalse(
                content.Contains("<Configuration>Debug</Configuration>", StringComparison.OrdinalIgnoreCase),
                $"{name} publishes a Debug build, which would ship the development store substitution.");

            if (!name.EndsWith("-selfcontained.pubxml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            StringAssert.Contains(
                content,
                "<Configuration>Release</Configuration>",
                $"{name} is a shipped publish profile but does not force Release, so publishing it would ship the "
                + "development store substitution.");
        }

        Assert.IsTrue(
            profiles.Count(path => Path.GetFileName(path).EndsWith("-selfcontained.pubxml", StringComparison.OrdinalIgnoreCase)) >= 2,
            "The documented publish profiles for the application and the service were not both found.");
    }

    [TestMethod]
    public void The_Fallback_Source_Is_Entirely_Inside_The_Guard()
    {
        var source = Read("MTM_Waitlist.Core/Services/MySqlHostFallback.cs");

        var lines = source
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .ToArray();

        var guardStart = Array.FindIndex(lines, line => line.Trim() == $"#if {Guard}");
        var guardEnd = Array.FindLastIndex(lines, line => line.Trim() == "#endif");

        Assert.IsTrue(guardStart >= 0, "MySqlHostFallback.cs no longer starts its development-only code with the guard.");
        Assert.IsTrue(guardEnd > guardStart, "MySqlHostFallback.cs has no closing guard.");

        var unguardedCode = lines
            .Where((line, index) => index < guardStart || index > guardEnd)
            .Where(line => line.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.AreEqual(
            0,
            unguardedCode.Length,
            "A localhost default appears outside the guard, so it would be compiled into a published build.");
    }

    [TestMethod]
    public void No_Shipped_File_Calls_The_Fallback_Directly()
    {
        var testRoot = Path.Combine(RepositoryRoot(), "MTM_Waitlist.Tests");

        // The seam is the one place allowed to name it, and its own mention sits inside the guard. The helper's
        // own file is not a caller. Prose that merely refers to the helper is stripped first, so a comment cannot
        // trip this.
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MTM_Waitlist.Core/Services/StoreHostResolution.cs",
            "MTM_Waitlist.Core/Services/MySqlHostFallback.cs",
        };

        var offenders = RepositoryFiles("*.cs")
            .Where(path => !path.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !allowed.Contains(
                Path.GetRelativePath(RepositoryRoot(), path).Replace(Path.DirectorySeparatorChar, '/')))
            .Where(path => Regex.IsMatch(StripComments(File.ReadAllText(path)), @"\bMySqlHostFallback\b"))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();

        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            offenders,
            "A shipped file calls MySqlHostFallback directly. Every caller goes through StoreHostResolution, which is what "
            + "makes the published behaviour decidable by reading one file: "
            + string.Join(", ", offenders));
    }

    /// <summary>Removes line and block comments, so prose about the helper is not read as a call to it.</summary>
    private static string StripComments(string source) =>
        Regex.Replace(
            Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline),
            @"//.*?$",
            " ",
            RegexOptions.Multiline);

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static IEnumerable<string> RepositoryFiles(string pattern) =>
        Directory.EnumerateFiles(RepositoryRoot(), pattern, SearchOption.AllDirectories);

    /// <summary>The repository root: the folder holding the solution, found by walking up from the test binary.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "The repository root could not be found from the test binary.");
        return directory!.FullName;
    }
}
