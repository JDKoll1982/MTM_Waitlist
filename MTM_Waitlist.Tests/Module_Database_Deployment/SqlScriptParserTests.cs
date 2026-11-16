using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Database_Deployment.Services;

namespace MTM_Waitlist.Tests.Module_Database_Deployment;

/// <summary>
/// How a script is cut into statements before any of it runs.
/// </summary>
/// <remarks>
/// This is the part of the tool that can silently destroy a working procedure: a body split at the wrong semicolon
/// arrives as half a statement and the server rejects it, while the file on disk still looks perfect. Each test here
/// is one of the shapes that makes naive splitting wrong.
/// </remarks>
[TestClass]
public sealed class Service_SqlScriptParserTests
{
    [TestMethod]
    public void Service_SqlScriptParserTests_Split_OneStatementIsOneStatement()
    {
        var statements = Service_SqlScriptParser.SplitStatements("SELECT 1;");

        Assert.AreEqual(1, statements.Count);
        Assert.AreEqual("SELECT 1", statements[0]);
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_ABareCreateProcedureBodyStaysWhole()
    {
        const string Script = """
            DROP PROCEDURE IF EXISTS `sp_thing`;
            CREATE PROCEDURE `sp_thing`()
            BEGIN
                SELECT 1;
                SELECT 2;
            END;
            """;

        var statements = Service_SqlScriptParser.SplitStatements(Script);

        Assert.AreEqual(2, statements.Count, "the drop and the whole body are two statements, never four");
        StringAssert.Contains(statements[1], "SELECT 2", "the body kept its second statement");
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_ADelimiterBlockArrivesAsOneCommand()
    {
        const string Script = """
            DELIMITER $$
            DROP PROCEDURE IF EXISTS `sp_thing` $$
            CREATE PROCEDURE `sp_thing`()
            BEGIN
                SELECT 1;
            END $$
            DELIMITER ;
            """;

        var statements = Service_SqlScriptParser.SplitStatements(Script);

        Assert.AreEqual(2, statements.Count, "the DELIMITER lines are instructions, not statements");
        StringAssert.StartsWith(statements[0], "DROP PROCEDURE");
        StringAssert.Contains(statements[1], "BEGIN");
        Assert.IsFalse(statements[1].Contains("$$", StringComparison.Ordinal), "the terminator is not part of the command");
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_ASemicolonInsideALiteralNeverSplits()
    {
        var statements = Service_SqlScriptParser.SplitStatements("INSERT INTO t (note) VALUES ('a;b');");

        Assert.AreEqual(1, statements.Count);
        StringAssert.Contains(statements[0], "'a;b'");
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_AnEscapedQuoteKeepsTheLiteralOpen()
    {
        var statements = Service_SqlScriptParser.SplitStatements(@"INSERT INTO t (note) VALUES ('it''s; fine');");

        Assert.AreEqual(1, statements.Count);
        StringAssert.Contains(statements[0], "it''s; fine");
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_CommentsStayWithTheirStatementAndNeverSplitIt()
    {
        const string Script = """
            -- a leading comment; with a semicolon
            # a hash comment; too
            /* a block comment; also */
            SELECT 1;
            """;

        var statements = Service_SqlScriptParser.SplitStatements(Script);

        Assert.AreEqual(1, statements.Count, "no comment may end the statement it introduces");
        StringAssert.Contains(statements[0], "SELECT 1");
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_AStatementWithNoFinalDelimiterIsStillAStatement()
    {
        var statements = Service_SqlScriptParser.SplitStatements("SELECT 42");

        Assert.AreEqual(1, statements.Count);
        Assert.AreEqual("SELECT 42", statements[0]);
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_AnEmptyScriptYieldsNothing()
    {
        Assert.AreEqual(0, Service_SqlScriptParser.SplitStatements(string.Empty).Count);
        Assert.AreEqual(0, Service_SqlScriptParser.SplitStatements("   \r\n  ").Count);
    }

    [TestMethod]
    public void Service_SqlScriptParserTests_Split_AQualifiedFunctionNameIsNotMistakenForADelimiter()
    {
        // The literal text of the old `USE` rule and an identifier that merely contains the word must not be read as
        // an instruction.
        var statements = Service_SqlScriptParser.SplitStatements("SELECT delimiter_count FROM t;");

        Assert.AreEqual(1, statements.Count);
        StringAssert.Contains(statements[0], "delimiter_count");
    }
}
