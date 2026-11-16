using System.Text;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Splits a MySQL script file into individual statements, honouring the client-side
/// DELIMITER directive so procedure/trigger bodies survive intact.
/// </summary>
public static class Service_SqlScriptParser
{
    public static IReadOnlyList<string> SplitStatements(string script)
    {
        var statements = new List<string>();
        if (string.IsNullOrWhiteSpace(script))
        {
            return statements;
        }

        var current = new StringBuilder();
        var delimiter = ";";
        var index = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inBacktick = false;
        var inLineComment = false;
        var inBlockComment = false;
        var bodyDepth = 0;

        void Flush()
        {
            var statement = current.ToString().Trim();
            current.Clear();
            if (string.IsNullOrWhiteSpace(statement))
            {
                return;
            }

            if (statement.All(character => char.IsWhiteSpace(character) || character == ';'))
            {
                return;
            }

            // A buffer holding nothing but comments is the file's header, not a command. Sending it would be a
            // statement the server has to answer for no reason, and it hides the drop that follows it.
            if (IsOnlyComments(statement))
            {
                return;
            }

            statements.Add(statement);
        }

        while (index < script.Length)
        {
            var character = script[index];

            if (inLineComment)
            {
                current.Append(character);
                if (character == '\n')
                {
                    inLineComment = false;
                }

                index++;
                continue;
            }

            if (inBlockComment)
            {
                current.Append(character);
                if (character == '*' && index + 1 < script.Length && script[index + 1] == '/')
                {
                    current.Append('/');
                    index += 2;
                    inBlockComment = false;
                    continue;
                }

                index++;
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote && !inBacktick)
            {
                if (character == '-' && index + 1 < script.Length && script[index + 1] == '-'
                    && (index + 2 >= script.Length || char.IsWhiteSpace(script[index + 2])))
                {
                    inLineComment = true;
                    current.Append("--");
                    index += 2;
                    continue;
                }
                if (character == '#')
                {
                    inLineComment = true;
                    current.Append(character);
                    index++;
                    continue;
                }

                if (character == '/' && index + 1 < script.Length && script[index + 1] == '*')
                {
                    inBlockComment = true;
                    current.Append("/*");
                    index += 2;
                    continue;
                }

                if (IsLineStart(script, index))
                {
                    var lineEnd = script.IndexOf('\n', index);
                    var line = lineEnd < 0 ? script[index..] : script[index..lineEnd];
                    var trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = trimmedLine["DELIMITER".Length..].Trim();
                        Flush();
                        if (token.Length > 0)
                        {
                            delimiter = token;
                        }

                        index = lineEnd < 0 ? script.Length : lineEnd + 1;
                        continue;
                    }
                }

                // A compound body written without a DELIMITER block is still one command. The depth is tracked so a
                // script that forgets the wrapper deploys correctly instead of arriving as fragments the server
                // rejects one by one.
                if (char.IsLetter(character) || character == '_')
                {
                    var wordEnd = index;
                    while (wordEnd < script.Length && (char.IsLetterOrDigit(script[wordEnd]) || script[wordEnd] == '_'))
                    {
                        wordEnd++;
                    }

                    var word = script[index..wordEnd];

                    if (string.Equals(word, "BEGIN", StringComparison.OrdinalIgnoreCase))
                    {
                        bodyDepth++;
                    }
                    else if (string.Equals(word, "END", StringComparison.OrdinalIgnoreCase)
                        && !FollowsControlKeyword(script, wordEnd))
                    {
                        // END IF / END LOOP / END WHILE / END CASE / END REPEAT close a control block, not the body.
                        bodyDepth = Math.Max(0, bodyDepth - 1);
                    }

                    current.Append(word);
                    index = wordEnd;
                    continue;
                }

                if (bodyDepth == 0
                    && string.CompareOrdinal(script, index, delimiter, 0, delimiter.Length) == 0)
                {
                    index += delimiter.Length;
                    Flush();
                    continue;
                }
            }

            if (character == '\'' && !inDoubleQuote && !inBacktick)
            {
                inSingleQuote = !inSingleQuote;
                current.Append(character);
                index++;
                continue;
            }

            if (character == '"' && !inSingleQuote && !inBacktick)
            {
                inDoubleQuote = !inDoubleQuote;
                current.Append(character);
                index++;
                continue;
            }

            if (character == '`' && !inSingleQuote && !inDoubleQuote)
            {
                inBacktick = !inBacktick;
                current.Append(character);
                index++;
                continue;
            }

            if (character == '\\' && (inSingleQuote || inDoubleQuote))
            {
                current.Append(character);
                index++;
                if (index < script.Length)
                {
                    current.Append(script[index]);
                    index++;
                }

                continue;
            }

            current.Append(character);
            index++;
        }

        Flush();
        return statements;
    }

    /// <summary>
    /// Whether a buffer holds nothing but comments, so it is a file header rather than a command.
    /// </summary>
    /// <remarks>
    /// Scanned rather than pattern-matched: a header routinely quotes code inside its comments, so a `/*` that
    /// appears inside a `--` line must not be treated as the start of a block.
    /// </remarks>
    private static bool IsOnlyComments(string statement)
    {
        var index = 0;
        while (index < statement.Length)
        {
            var character = statement[index];
            if (char.IsWhiteSpace(character) || character == ';')
            {
                index++;
                continue;
            }

            if (character == '#'
                || (character == '-' && index + 1 < statement.Length && statement[index + 1] == '-'))
            {
                while (index < statement.Length && statement[index] != '\n')
                {
                    index++;
                }

                continue;
            }

            if (character == '/' && index + 1 < statement.Length && statement[index + 1] == '*')
            {
                var close = statement.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = close < 0 ? statement.Length : close + 2;
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the word following <paramref name="index"/> closes a control block rather than a body.
    /// </summary>
    /// <remarks>
    /// <c>END IF</c>, <c>END LOOP</c>, <c>END WHILE</c>, <c>END CASE</c>, <c>END REPEAT</c> and <c>END FOR</c> close
    /// the construct they belong to. Only a bare <c>END</c> closes the routine body the depth is counting.
    /// </remarks>
    private static bool FollowsControlKeyword(string script, int index)
    {
        var cursor = index;
        while (cursor < script.Length && char.IsWhiteSpace(script[cursor]))
        {
            cursor++;
        }

        foreach (var keyword in s_controlKeywords)
        {
            if (cursor + keyword.Length <= script.Length
                && string.Compare(script, cursor, keyword, 0, keyword.Length, ignoreCase: true, culture: null) == 0
                && (cursor + keyword.Length == script.Length
                    || !char.IsLetterOrDigit(script[cursor + keyword.Length])))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The words that make an <c>END</c> belong to a control block.</summary>
    private static readonly string[] s_controlKeywords = ["IF", "LOOP", "WHILE", "CASE", "REPEAT", "FOR"];

    private static bool IsLineStart(string script, int index)    {
        for (var position = index - 1; position >= 0; position--)
        {
            var character = script[position];
            if (character == '\n')
            {
                return true;
            }

            if (!char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }
}
