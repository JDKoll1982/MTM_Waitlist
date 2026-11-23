using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The Infor Visual queue scripts are pasted into a SQL client by a person as well as run by the application, so
/// they have to parse as a batch on their own.
/// </summary>
/// <remarks>
/// A common table expression must be preceded by a <b>terminated</b> statement. A previous line that ends in an
/// <c>END</c> with no semicolon is enough to make SQL Server answer "Incorrect syntax near the keyword 'with'", and
/// the caller then reports the read as unavailable — which is precisely how the generic die search failed on first
/// keystroke while every other queue script answered (defect, 2026-11-23). This scan is the guard the compiler cannot
/// be: the scripts are text at run time, so nothing else checks their syntax before the plant is asked.
/// </remarks>
[TestClass]
public sealed class VisualQueueScriptAuditTests
{
    private static readonly Regex CteStart = new(@"^\s*WITH\s+\w+\s+AS\s*\(?\s*$", RegexOptions.Compiled);

    [TestMethod]
    public void Audit_VisualQueueScriptAuditTests_EveryCteFollowsATerminatedStatement()
    {
        var scriptsDirectory = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "InforVisual",
            "Queues");

        var offenders = new List<string>();

        foreach (var script in Directory.EnumerateFiles(scriptsDirectory, "*.sql", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(script);

            for (var index = 0; index < lines.Length; index++)
            {
                if (!CteStart.IsMatch(lines[index]))
                {
                    continue;
                }

                var previous = PreviousMeaningfulLine(lines, index);
                if (previous is not null && !previous.EndsWith(';'))
                {
                    offenders.Add(
                        $"{Path.GetFileName(script)}:{index + 1} follows '{previous}', which is not terminated with a semicolon");
                }
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "a CTE after an unterminated statement fails to parse: " + string.Join(" | ", offenders));
    }

    /// <summary>The last line that is neither blank nor a comment, or null when the CTE opens the batch.</summary>
    private static string? PreviousMeaningfulLine(string[] lines, int index)
    {
        for (var candidate = index - 1; candidate >= 0; candidate--)
        {
            var text = lines[candidate].Trim();

            if (text.Length == 0 || text.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            return text;
        }

        return null;
    }
}
