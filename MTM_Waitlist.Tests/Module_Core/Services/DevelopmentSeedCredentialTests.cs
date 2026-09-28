using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Services;

/// <summary>
/// The credential the development seed writes for the accounts it creates, read from the seed file the repository
/// ships rather than from a copy of the value in the test (FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect these pin.</b> The seed used to write the retired marker <c>'0000'</c> as the stored hash with a
/// null salt. Nothing can confirm such a row: <see cref="PasswordSecretHasher.Verify"/> refuses a null salt
/// before it derives anything, so every seeded account was unable to sign in — at the machine-setup gate, at the
/// sign-in screen, everywhere. A fresh install had no way in, and nothing in the build said so.
/// </para>
/// <para>
/// <b>The credential is a deliberate shared placeholder.</b> A seed is a static file, so the value it writes has
/// to be a literal, and every seeded account uses the same one. It is a development placeholder rather than a
/// secret and belongs to no real person — which is why it is checked here as the thing a fresh install signs in
/// with, and why the retired marker is asserted absent as a <i>hash</i> rather than as a value.
/// </para>
/// <para>
/// The hash is checked with the application's own comparison, so a seed that wrote a digest from some other
/// scheme — or from the same scheme with different parameters — fails here rather than at a sign-in prompt.
/// </para>
/// </remarks>
[TestClass]
public sealed class DevelopmentSeedCredentialTests
{
    /// <summary>The shipped seed that creates the development accounts, relative to the repository root.</summary>
    private const string SeedRelativePath = "Database/Seeds/seed_dev_masked_baseline/create.sql";

    /// <summary>The credential the seed issues to every account it creates.</summary>
    private const string DevelopmentCredential = "4330";

    /// <summary>
    /// The retired marker the seed used to store where a digest belongs. It is not a credential: with a null salt
    /// nothing could confirm it, which is the defect this file exists for.
    /// </summary>
    private const string RetiredMarker = "0000";

    /// <summary>
    /// One account row of the seed's <c>core_users_profiles</c> insert: the sign-in name it creates and the two
    /// credential columns it writes for that account.
    /// </summary>
    /// <param name="SignInName">The <c>username_normalized</c> value, as the seed spells it.</param>
    /// <param name="StoredHash">The <c>password_hash</c> token, or <c>NULL</c> when the seed writes none.</param>
    /// <param name="StoredSalt">The <c>password_salt</c> token, or <c>NULL</c> when the seed writes none.</param>
    private readonly record struct SeededAccount(string SignInName, string StoredHash, string StoredSalt);

    /// <summary>The accounts the seed is expected to create, so a parse that matched nothing cannot pass.</summary>
    private static readonly string[] ExpectedSignInNames =
    [
        "johnk",
        "jkoll",
        "test.admin",
        "test.developer",
        "test.plant.manager",
        "test.setup.lead",
        "test.production.lead",
        "test.setup",
        "test.production",
        "test.material.handler",
    ];

    [TestMethod]
    public void TheShippedSeed_CreatesTheDevelopmentAccountsItIsExpectedTo()
    {
        var accounts = SeededAccounts();

        CollectionAssert.AreEquivalent(
            ExpectedSignInNames,
            accounts.Select(account => account.SignInName).ToArray(),
            "The seed's account set changed, so what the credential assertions below cover has changed with it.");
    }

    [TestMethod]
    public void TheShippedSeed_GivesEveryAccountItCreatesACredentialTheApplicationConfirms()
    {
        var accounts = SeededAccounts();

        Assert.IsTrue(accounts.Count > 0, "no account row was parsed, so this check would prove nothing");

        foreach (var account in accounts)
        {
            Assert.AreNotEqual("NULL", account.StoredSalt, $"{account.SignInName} is seeded with no salt, so no credential can ever confirm it");
            Assert.AreNotEqual("NULL", account.StoredHash, $"{account.SignInName} is seeded with no credential at all");

            var salt = Convert.FromHexString(account.StoredSalt);
            Assert.AreEqual(
                PasswordSecretHasher.SaltLengthBytes,
                salt.Length,
                $"{account.SignInName} is seeded with a salt of the wrong length, so the digest can never match");

            Assert.IsTrue(
                PasswordSecretHasher.Verify(DevelopmentCredential, account.StoredHash, salt),
                $"{account.SignInName} cannot sign in with the credential the seed is documented to issue, so a fresh install has no way in");
        }
    }

    [TestMethod]
    public void TheShippedSeed_DoesNotStoreTheRetiredPlaceholderMarkerAsAHash()
    {
        // The marker itself is the defect: it is a four-character literal standing where a base64 digest belongs.
        foreach (var account in SeededAccounts())
        {
            Assert.AreNotEqual(
                RetiredMarker,
                account.StoredHash,
                $"{account.SignInName} still stores the retired marker as its hash, which no comparison can confirm");
        }
    }

    /// <summary>Reads and parses the accounts out of the shipped seed.</summary>
    private static List<SeededAccount> SeededAccounts()
    {
        var seedPath = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            SeedRelativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.IsTrue(File.Exists(seedPath), $"the shipping seed is missing: {SeedRelativePath}");

        var seed = File.ReadAllText(seedPath);

        // Only the account insert is of interest: the role catalogue insert above it also uses UUID() rows, and
        // the table is named once more by the TRUNCATE that clears it. Anchoring on the statement rather than on
        // the table name is what keeps this pointed at the accounts.
        var insert = Regex.Match(seed, @"INSERT\s+INTO\s+core_users_profiles");
        Assert.IsTrue(insert.Success, "the seed no longer inserts into core_users_profiles");

        var start = insert.Index;

        var end = seed.IndexOf("ON DUPLICATE KEY UPDATE", start, StringComparison.Ordinal);
        var block = end > start ? seed[start..end] : seed[start..];

        // Every account row carries the same twelve values, whether it is written across lines or on one:
        // UUID(), sign-in name, first name, last name, hash, salt, then the rest. The first two accounts are
        // written one value per line and the rest on a single line each, so the opening parenthesis and the
        // UUID() are separated by whitespace in one layout and not in the other.
        var matches = Regex.Matches(
            block,
            @"\(\s*UUID\(\),\s*'(?<name>[^']+)',\s*'[^']*',\s*'[^']*',\s*(?<hash>NULL|'[^']*'),\s*(?<salt>NULL|UNHEX\('[0-9A-Fa-f]*'\))");

        var accounts = new List<SeededAccount>(matches.Count);

        foreach (Match match in matches)
        {
            accounts.Add(new SeededAccount(
                match.Groups["name"].Value,
                Unquote(match.Groups["hash"].Value),
                SaltHex(match.Groups["salt"].Value)));
        }

        return accounts;
    }

    /// <summary>A SQL string literal without its quotes, or the word <c>NULL</c> as written.</summary>
    private static string Unquote(string token)
        => token.StartsWith('\'') && token.EndsWith('\'') ? token[1..^1] : token;

    /// <summary>The hex digits of an <c>UNHEX('…')</c> literal, or the word <c>NULL</c> as written.</summary>
    private static string SaltHex(string token)
    {
        const string prefix = "UNHEX('";

        return token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? token[prefix.Length..token.LastIndexOf('\'')]
            : token;
    }
}
