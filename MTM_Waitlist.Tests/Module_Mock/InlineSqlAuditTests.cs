using System.Text;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// SC-013's SP-first gate: fails if MySQL statement text survives in application code (constitution III —
/// "Every data operation MUST go through a stored procedure; inline or hard-coded SQL statement text MUST
/// NOT remain in application code").
/// </summary>
/// <remarks>
/// <para>
/// <b>What "application code" means here.</b> Every <c>.cs</c> file in the repository except the test
/// project. The integration suites under <c>MTM_Waitlist.Tests/</c> deliberately seed and inspect rows with
/// raw statements so a converted call site can be proven against the live server; that is test fixture
/// setup, not application behaviour, and it is excluded for the same reason the retired-symbol audit keeps
/// its own file out of scope.
/// </para>
/// <para>
/// <b>Why markers rather than call sites.</b> A stored-procedure invocation carries no statement text at
/// all — it is a procedure-name constant passed with <c>CommandType.StoredProcedure</c> — so the presence of
/// a statement marker is the property to forbid, and it is the property that survives every refactor (a new
/// helper, a raw <c>MySqlCommand</c>, a script loaded and executed). Markers are matched case-sensitively:
/// SQL keywords are written upper-case throughout this repository, while prose and identifiers that merely
/// start with the same letters ("Select a workstation…", "selected_dunnage_parts_json") are not statements
/// and must not be reported.
/// </para>
/// <para>
/// <b>The wrong-helper-for-DML shape.</b> A marker scan alone will not catch a <c>rows.Count</c> read taken
/// from a row-returning helper after a <c>DELETE</c>/<c>UPDATE</c> (a defect found three times during the
/// SP-first conversion). That shape is only reachable when a DML statement exists somewhere to be routed —
/// so the second assertion below pins the raw-SQL seams (<c>ExecuteSqlQueryAsync</c> /
/// <c>ExecuteSqlNonQueryAsync</c>) to a reviewed allowlist. A new raw-SQL call site fails this audit instead
/// of silently reopening the defect.
/// </para>
/// </remarks>
[TestClass]
public sealed class InlineSqlAuditTests
{
    /// <summary>MySQL statement markers that must not appear in application code.</summary>
    private static readonly (string Description, Regex Pattern)[] s_statementMarkers =
    [
        ("SELECT statement", new Regex(@"(?<![A-Za-z0-9_])SELECT\s", RegexOptions.Compiled)),
        ("INSERT statement", new Regex(@"(?<![A-Za-z0-9_])INSERT\s+INTO\b", RegexOptions.Compiled)),
        ("UPDATE statement", new Regex(@"(?<![A-Za-z0-9_])UPDATE\s+[A-Za-z_][A-Za-z0-9_]*\s+SET\b", RegexOptions.Compiled)),
        ("DELETE statement", new Regex(@"(?<![A-Za-z0-9_])DELETE\s+FROM\b", RegexOptions.Compiled)),
        ("CALL statement", new Regex(@"(?<![A-Za-z0-9_])CALL\s+[A-Za-z_]", RegexOptions.Compiled)),
        ("DDL statement", new Regex(@"(?<![A-Za-z0-9_])(CREATE|DROP|ALTER|TRUNCATE|RENAME)\s+(TABLE|DATABASE|PROCEDURE|FUNCTION|INDEX|VIEW)\b", RegexOptions.Compiled)),
    ];

    /// <summary>Directories that hold build output, documentation, or tooling rather than application code.</summary>
    private static readonly string[] s_excludedDirectories =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.specify{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}MTM_Waitlist.Tests{Path.DirectorySeparatorChar}",
    ];

    /// <summary>
    /// The reviewed raw-SQL seams: the only application files allowed to reference the row-returning and
    /// non-query statement seams. Everything else must go through a stored procedure.
    /// </summary>
    private static readonly Dictionary<string, string> s_rawSqlSeamAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MTM_Waitlist.Core/Contracts/Services/IMySqlHelperServer.cs"] =
            "the seam's contract.",
        ["MTM_Waitlist.Core/Services/MySqlHelperServer.cs"] =
            "the seam's implementation.",
        ["MTM_Waitlist.Core/Services/WipFloorInventoryService.cs"] =
            "reads the checked-in MTMWipApp queue script GetWipFloorQuantities.sql; the C# carries the script "
            + "name only and never statement text (Discovery/03 §C — stays live).",
    };

    /// <summary>
    /// Fails when any MySQL statement marker survives in application code (SC-013).
    /// </summary>
    [TestMethod]
    public void NoMySqlStatementTextRemainsInApplicationCode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var file in EnumerateApplicationSourceFiles(repositoryRoot))
        {
            // Comments are removed first: a doc comment that explains which statement an artifact
            // replaced is documentation of the removal, not statement text in application code.
            var code = StripComments(File.ReadAllText(file));
            var relativePath = ToRelativePath(repositoryRoot, file);

            foreach (var (description, pattern) in s_statementMarkers)
            {
                if (pattern.IsMatch(code))
                {
                    violations.Add($"{relativePath}: {description} ({pattern})");
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "SC-013 / constitution III require every data operation to go through a stored procedure; "
                + "application code must carry no inline or hard-coded statement text:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Fails when a raw-SQL seam is referenced by application code that is not on the reviewed allowlist,
    /// which is what keeps a DML statement out of the row-returning helper (the wrong-helper-for-DML shape).
    /// </summary>
    [TestMethod]
    public void RawSqlSeamIsConfinedToItsReviewedAllowlist()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var file in EnumerateApplicationSourceFiles(repositoryRoot))
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("ExecuteSqlQueryAsync", StringComparison.Ordinal)
                && !content.Contains("ExecuteSqlNonQueryAsync", StringComparison.Ordinal))
            {
                continue;
            }

            var relativePath = ToRelativePath(repositoryRoot, file);
            if (!s_rawSqlSeamAllowlist.ContainsKey(relativePath))
            {
                violations.Add(relativePath);
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "These application files call a raw-SQL seam without being reviewed and recorded in "
                + $"{nameof(InlineSqlAuditTests)}.{nameof(s_rawSqlSeamAllowlist)}. DML must go through "
                + "ExecuteStoredProcedureNonQueryAsync (which reports the affected-row count); route the "
                + "operation to a stored procedure instead of widening this list:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Every allowlist entry must still be a real file that still uses a seam, so the exemption cannot
    /// linger after the call site it describes is gone.
    /// </summary>
    [TestMethod]
    public void EveryRawSqlSeamAllowlistEntryIsStillAccurate()
    {
        var repositoryRoot = FindRepositoryRoot();
        var stale = new List<string>();

        foreach (var entry in s_rawSqlSeamAllowlist)
        {
            var path = Path.Combine([repositoryRoot, .. entry.Key.Split('/')]);
            if (!File.Exists(path))
            {
                stale.Add($"{entry.Key}: file no longer exists.");
                continue;
            }

            var content = File.ReadAllText(path);
            if (!content.Contains("ExecuteSqlQueryAsync", StringComparison.Ordinal)
                && !content.Contains("ExecuteSqlNonQueryAsync", StringComparison.Ordinal))
            {
                stale.Add($"{entry.Key}: no longer references a raw-SQL seam — remove the exemption.");
            }
        }

        Assert.AreEqual(
            0,
            stale.Count,
            "Stale raw-SQL seam exemptions:" + Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    private static IEnumerable<string> EnumerateApplicationSourceFiles(string repositoryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (s_excludedDirectories.Any(excluded => file.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // This audit necessarily contains the marker patterns it forbids.
            if (string.Equals(Path.GetFileName(file), "InlineSqlAuditTests.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return file;
        }
    }

    private static string ToRelativePath(string repositoryRoot, string file)
        => Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');

    /// <summary>
    /// Removes C# comments while preserving string-literal contents, so a marked statement is found
    /// wherever it is written in code but never reported from prose.
    /// </summary>
    /// <param name="source">The raw file text.</param>
    /// <returns>The file text with every comment removed.</returns>
    private static string StripComments(string source)
    {
        var builder = new StringBuilder(source.Length);
        var index = 0;

        while (index < source.Length)
        {
            var current = source[index];

            if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                while (index < source.Length && source[index] != '\n')
                {
                    index++;
                }

                continue;
            }

            if (current == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                index += 2;
                while (index + 1 < source.Length && !(source[index] == '*' && source[index + 1] == '/'))
                {
                    index++;
                }

                index = Math.Min(index + 2, source.Length);
                continue;
            }

            // @"..." and @$"..." verbatim strings; "" is an escaped quote inside them.
            if (current == '@' && index + 1 < source.Length && source[index + 1] == '"')
            {
                builder.Append(' ');
                index = CopyQuoted(source, index + 1, builder, escapesBackslash: false);
                continue;
            }

            if (current == '$')
            {
                var quoteIndex = index + 1;
                if (quoteIndex < source.Length && source[quoteIndex] == '@')
                {
                    quoteIndex++;
                }

                if (quoteIndex < source.Length && source[quoteIndex] == '"')
                {
                    // Both $@"..." and @$"..." are verbatim, so "" is the escape inside them.
                    var verbatim = quoteIndex > 0 && source[quoteIndex - 1] == '@';
                    builder.Append(' ');
                    index = IsRawStringStart(source, quoteIndex)
                        ? CopyRawString(source, quoteIndex, builder)
                        : CopyQuoted(source, quoteIndex, builder, escapesBackslash: !verbatim);
                    continue;
                }

                builder.Append(current);
                index++;
                continue;
            }

            if (current == '"')
            {
                builder.Append(' ');
                index = IsRawStringStart(source, index)
                    ? CopyRawString(source, index, builder)
                    : CopyQuoted(source, index, builder, escapesBackslash: true);
                continue;
            }

            // A character literal may contain a quote or a slash; copying it verbatim keeps the
            // scanner from mistaking its interior for a string or a comment.
            if (current == '\'')
            {
                builder.Append(current);
                index++;
                while (index < source.Length)
                {
                    if (source[index] == '\\' && index + 1 < source.Length)
                    {
                        builder.Append(source[index]).Append(source[index + 1]);
                        index += 2;
                        continue;
                    }

                    builder.Append(source[index]);
                    if (source[index] == '\'')
                    {
                        index++;
                        break;
                    }

                    index++;
                }

                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static bool IsRawStringStart(string source, int quoteIndex)
        => quoteIndex + 2 < source.Length && source[quoteIndex + 1] == '"' && source[quoteIndex + 2] == '"';

    /// <summary>
    /// Copies a string literal's contents (the statement text lives here) and returns the index just
    /// past its closing quote.
    /// </summary>
    private static int CopyQuoted(string source, int quoteIndex, StringBuilder builder, bool escapesBackslash)
    {
        var index = quoteIndex + 1;

        while (index < source.Length)
        {
            var current = source[index];

            if (escapesBackslash && current == '\\' && index + 1 < source.Length)
            {
                builder.Append(current).Append(source[index + 1]);
                index += 2;
                continue;
            }

            if (current == '"')
            {
                if (index + 1 < source.Length && source[index + 1] == '"'
                    && !escapesBackslash)
                {
                    builder.Append('"');
                    index += 2;
                    continue;
                }

                return index + 1;
            }

            builder.Append(current);
            index++;
        }

        return index;
    }

    /// <summary>
    /// Copies a raw string literal's contents and returns the index just past its closing quotes.
    /// </summary>
    private static int CopyRawString(string source, int quoteIndex, StringBuilder builder)
    {
        var contentStart = quoteIndex + 3;
        var terminator = source.IndexOf("\"\"\"", contentStart, StringComparison.Ordinal);

        if (terminator < 0)
        {
            builder.Append(source, contentStart, source.Length - contentStart);
            return source.Length;
        }

        builder.Append(source, contentStart, terminator - contentStart);
        return terminator + 3;
    }

    /// <summary>
    /// Walks up from the test binaries until the solution file is found.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"The repository root could not be located above '{AppContext.BaseDirectory}'.");
        return AppContext.BaseDirectory;
    }
}
