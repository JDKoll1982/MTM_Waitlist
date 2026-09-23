using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The role rename proved rather than asserted (T033, FR-013, FR-014, SC-005, SC-006, §6 gate 3).
/// </summary>
/// <remarks>
/// <para>
/// <b>One run, three passes.</b> This is a single test because the three passes are one sequence and share one
/// starting state: apply the rename and assert the accounts moved, actually run the paired reversal and assert
/// they moved back, then re-apply the rename and assert the store ends holding IT Department rather than the
/// retired role. Phase 5 seeds the permission baselines against <c>it_department</c>, so a run that stopped after
/// the reversal would leave the catalogue holding a role the baselines are not keyed to.
/// </para>
/// <para>
/// <b>The shipped scripts are what run.</b> The two passes execute
/// <c>Database/Seeds/seed_role_admin_to_it_department/create.sql</c> and its paired <c>rollback.sql</c> from the
/// repository, not a retyped copy of them. Those files are written for the <c>mysql</c> client, which needs
/// <c>DELIMITER</c> to know where a compound statement ends; the server needs no such directive, so the
/// statements are read out of the file and sent one at a time.
/// </para>
/// <para>
/// <b>The starting state is normalised first, by running the reversal.</b> A previous run of this test leaves the
/// store holding the renamed role, so the reversal is run before the first pass. The reversal moves every account
/// standing on IT Department, which is the window its own header records.
/// </para>
/// <para>
/// <b>Environment-gated on <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>.</b> Without a live <c>mtm_waitlist</c>
/// this test reports inconclusive, recorded as skipped and never as passing (test conventions, "Environment-gated
/// tests").
/// </para>
/// </remarks>
[TestClass]
public sealed class RoleRenameMigrationTests
{
    private const string ConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    private const string RetiredRoleCode = "admin";

    private const string RenamedRoleCode = "it_department";

    private const string RenamedRoleRank = "90";

    /// <summary>Every code the catalogue holds once the rename has run, including the renamed role.</summary>
    private static readonly string[] PinnedRoleCodes =
    {
        "developer", "it_department", "plant_manager", "production_lead", "setup_lead",
        "material_handler_lead", "material_handler", "production", "setup",
    };

    /// <summary>
    /// The switched-off holder this test owns. It is switched off by this test rather than found switched off,
    /// because activation is a separate act and the shipped baseline seeds every account active.
    /// </summary>
    private const string FixtureSignInName = "zz.migration.fixture";

    private const string ApplyScriptRelativePath =
        "Database/Seeds/seed_role_admin_to_it_department/create.sql";

    private const string ReversalScriptRelativePath =
        "Database/Seeds/seed_role_admin_to_it_department/rollback.sql";

    private MySqlHelperServer? _helper;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live rename proof.");
        }

        _helper = new MySqlHelperServer(
            Options.Create(new StartupDatabaseOptions { ConnectionString = connectionString! }));
    }

    [TestCleanup]
    public async Task TestCleanupAsync()
    {
        if (_helper is null)
        {
            return;
        }

        // The fixture is this test's own, so it goes whether the run passed or failed. The assignment goes first:
        // the foreign key from the assignment to the profile is RESTRICT.
        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE ra FROM auth_roles_assignments ra INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized = @p_username;"
                + " DELETE FROM core_users_profiles WHERE username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = FixtureSignInName },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    [TestMethod]
    public async Task TheRun_AppliesTheRenameReversesItAndReappliesIt()
    {
        var helper = _helper!;
        var catalogue = new RoleCatalogService(helper);

        // The reversal first, so the run starts from a state this test knows: the retired role in the catalogue.
        // It is a no-op when the store is already holding the retired role.
        await ApplyScriptAsync(ReversalScriptRelativePath);

        Assert.IsTrue(
            await RoleExistsAsync(RetiredRoleCode),
            "The run must start with the retired role in the catalogue; the reversal above should have put it back.");

        await WriteSwitchedOffFixtureHoldingAsync(RetiredRoleCode);

        var beforeTheRename = await SignInNamesOnRoleAsync(RetiredRoleCode);
        Assert.IsTrue(
            beforeTheRename.Length > 0,
            "The run needs at least one account holding the retired role to move.");
        CollectionAssert.Contains(
            beforeTheRename,
            FixtureSignInName,
            "The switched-off fixture must hold the retired role before the rename.");

        // ── Pass one: apply the rename ────────────────────────────────────────────────────────────────────────
        await ApplyScriptAsync(ApplyScriptRelativePath);

        CollectionAssert.AreEquivalent(
            beforeTheRename,
            await SignInNamesOnRoleAsync(RenamedRoleCode),
            "The set of accounts on IT Department must equal the set that held the administrator role.");
        Assert.AreEqual(
            0,
            (await SignInNamesWithNoRoleAsync()).Length,
            "No account may be left holding no role: the rename is one all-or-nothing act (FR-013).");
        Assert.IsTrue(
            await IsSwitchedOffAsync(FixtureSignInName),
            "A switched-off account that held the administrator role holds IT Department and stays switched off, because activation is a separate act.");
        Assert.IsFalse(
            await RoleExistsAsync(RetiredRoleCode),
            "The retired role must be gone from the catalogue.");
        Assert.AreEqual(
            RenamedRoleRank,
            await RoleRankAsync(RenamedRoleCode),
            "IT Department is written at its own rung rather than left on the column default.");

        var afterTheRename = await CatalogueCodesAsync(catalogue);
        CollectionAssert.AreEquivalent(
            PinnedRoleCodes,
            afterTheRename,
            "The catalogue read must return the nine pinned codes with the retired role absent and IT Department present.");

        // ── Pass two: actually run the paired reversal ────────────────────────────────────────────────────────
        await ApplyScriptAsync(ReversalScriptRelativePath);

        Assert.IsTrue(
            await RoleExistsAsync(RetiredRoleCode),
            "The reversal must put the administrator role back.");
        Assert.AreEqual(
            "0",
            await RoleRankAsync(RetiredRoleCode),
            "The reversal restores the rung that row held when the migration removed it, which is the column default.");
        Assert.IsFalse(
            await RoleExistsAsync(RenamedRoleCode),
            "The reversal must take IT Department out of the catalogue.");
        CollectionAssert.AreEquivalent(
            beforeTheRename,
            await SignInNamesOnRoleAsync(RetiredRoleCode),
            "The reversal must put those accounts back on the administrator role.");
        Assert.AreEqual(
            0,
            (await SignInNamesWithNoRoleAsync()).Length,
            "No account may be left holding no role after the reversal either.");

        // ── Pass three: re-apply the rename, which is the state Phase 5's baselines are keyed to ─────────────
        await ApplyScriptAsync(ApplyScriptRelativePath);

        Assert.IsFalse(
            await RoleExistsAsync(RetiredRoleCode),
            "The run must end with the retired role out of the catalogue.");
        Assert.AreEqual(
            RenamedRoleRank,
            await RoleRankAsync(RenamedRoleCode),
            "The run must end with IT Department back at its rung.");
        CollectionAssert.AreEquivalent(
            beforeTheRename,
            await SignInNamesOnRoleAsync(RenamedRoleCode),
            "The run must end with the accounts back on IT Department.");
        Assert.AreEqual(
            0,
            (await SignInNamesWithNoRoleAsync()).Length,
            "The run must end with no account holding no role.");

        var finalCatalogue = await CatalogueCodesAsync(catalogue);
        CollectionAssert.AreEquivalent(
            PinnedRoleCodes,
            finalCatalogue,
            "The run must end with the nine pinned codes and the retired role absent.");
    }

    /// <summary>
    /// Runs one of the paired migration scripts. Each statement is sent on its own, because the file is written
    /// for the <c>mysql</c> client: <c>DELIMITER</c> is a client directive the server neither needs nor accepts,
    /// and a compound body reaches the server as one statement.
    /// </summary>
    private async Task ApplyScriptAsync(string relativePath)
    {
        var path = Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(path), $"The shipped migration script is missing: {path}");

        foreach (var statement in StatementsOf(await File.ReadAllTextAsync(path)))
        {
            if (statement.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                await _helper!.ExecuteSqlQueryAsync(statement, new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist);
            }
            else
            {
                await _helper!.ExecuteSqlNonQueryAsync(statement, new Dictionary<string, object?>(), MySqlDatabaseTarget.MtmWaitlist);
            }
        }
    }

    /// <summary>
    /// The statements of a client script, delimiter directive removed and a plain semicolon put back. Blank lines
    /// and comment lines are dropped: they carry no statement.
    /// </summary>
    private static IReadOnlyList<string> StatementsOf(string script)
    {
        var statements = new List<string>();
        var pending = new System.Text.StringBuilder();
        var delimiter = ";";

        foreach (var line in script.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
            {
                delimiter = trimmed["DELIMITER ".Length..].Trim();
                continue;
            }

            pending.AppendLine(line);

            if (!trimmed.EndsWith(delimiter, StringComparison.Ordinal))
            {
                continue;
            }

            var text = pending.ToString();
            statements.Add(text[..text.LastIndexOf(delimiter, StringComparison.Ordinal)].Trim() + ";");
            pending.Clear();
        }

        Assert.AreEqual(string.Empty, pending.ToString().Trim(), "The script ends with a statement that is not terminated.");

        return statements;
    }

    /// <summary>
    /// Writes the fixture and gives it the one role it holds, switched off. Any role it already held is removed
    /// first, so the fixture holds exactly one role and no run can leave it holding two.
    /// </summary>
    private async Task WriteSwitchedOffFixtureHoldingAsync(string roleCode)
    {
        await _helper!.ExecuteSqlNonQueryAsync(
            "INSERT INTO core_users_profiles (public_id, username_normalized, first_name, last_name, password_hash, password_salt, require_password_change, display_name, employee_identifier, is_active, created_utc, updated_utc) "
                + "VALUES (@p_public_id, @p_username, 'Migration', 'Fixture', '0000', NULL, 1, 'Migration Fixture', '0099', 0, UTC_TIMESTAMP(), UTC_TIMESTAMP()) "
                + "ON DUPLICATE KEY UPDATE is_active = 0, updated_utc = UTC_TIMESTAMP();",
            new Dictionary<string, object?>
            {
                ["p_public_id"] = Guid.NewGuid().ToString(),
                ["p_username"] = FixtureSignInName,
            },
            MySqlDatabaseTarget.MtmWaitlist);

        await _helper.ExecuteSqlNonQueryAsync(
            "DELETE ra FROM auth_roles_assignments ra INNER JOIN core_users_profiles u ON u.id = ra.user_id WHERE u.username_normalized = @p_username;",
            new Dictionary<string, object?> { ["p_username"] = FixtureSignInName },
            MySqlDatabaseTarget.MtmWaitlist);

        await _helper.ExecuteSqlNonQueryAsync(
            "INSERT INTO auth_roles_assignments (public_id, user_id, role_id, assigned_utc, assigned_by_user_id) "
                + "SELECT UUID(), u.id, r.id, UTC_TIMESTAMP(), u.id FROM core_users_profiles u INNER JOIN auth_roles_catalog r ON r.role_code = @p_role_code "
                + "WHERE u.username_normalized = @p_username;",
            new Dictionary<string, object?>
            {
                ["p_role_code"] = roleCode,
                ["p_username"] = FixtureSignInName,
            },
            MySqlDatabaseTarget.MtmWaitlist);
    }

    private static async Task<string[]> CatalogueCodesAsync(RoleCatalogService catalogue)
    {
        // The service caches for the session and the catalogue changes under it three times in this run, so the
        // cache is emptied before each read rather than after it.
        catalogue.Invalidate();

        var roles = await catalogue.GetRolesAsync();

        return roles.Select(entry => entry.RoleCode).OrderBy(code => code, StringComparer.Ordinal).ToArray();
    }

    private async Task<bool> RoleExistsAsync(string roleCode)
        => (await ScalarsAsync("SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = @p_role_code;", "p_role_code", roleCode))
            .Single() != "0";

    private async Task<string> RoleRankAsync(string roleCode)
        => (await ScalarsAsync("SELECT r.role_rank FROM auth_roles_catalog r WHERE r.role_code = @p_role_code;", "p_role_code", roleCode))
            .Single();

    /// <summary>
    /// Whether the fixture is switched off, read through the same helper the rest of the suite uses.
    /// </summary>
    /// <remarks>
    /// The provider reads <c>TINYINT(1)</c> as a <see cref="bool"/> by default, so the flag arrives here as
    /// "True"/"False" rather than as "1"/"0". Both spellings are accepted, because the column's width is a
    /// storage detail and the question is the flag's value. This cost one failed run to find: the first version
    /// compared the text to "0" and reported a switched-off account as switched on.
    /// </remarks>
    private async Task<bool> IsSwitchedOffAsync(string signInName)
    {
        var value = (await ScalarsAsync(
            "SELECT u.is_active FROM core_users_profiles u WHERE u.username_normalized = @p_value;",
            "p_value",
            signInName)).Single();

        return value switch
        {
            "1" or "True" or "true" => false,
            "0" or "False" or "false" => true,
            _ => throw new InvalidOperationException($"'{value}' is not an active flag."),
        };
    }

    private async Task<string[]> SignInNamesOnRoleAsync(string roleCode)
        => (await ScalarsAsync(
                "SELECT u.username_normalized FROM auth_roles_assignments ra "
                    + "INNER JOIN auth_roles_catalog r ON r.id = ra.role_id "
                    + "INNER JOIN core_users_profiles u ON u.id = ra.user_id "
                    + "WHERE r.role_code = @p_role_code ORDER BY u.username_normalized;",
                "p_role_code",
                roleCode))
            .ToArray();

    private async Task<string[]> SignInNamesWithNoRoleAsync()
        => (await ScalarsAsync(
                "SELECT u.username_normalized FROM core_users_profiles u "
                    + "WHERE NOT EXISTS (SELECT 1 FROM auth_roles_assignments ra WHERE ra.user_id = u.id) "
                    + "ORDER BY u.username_normalized;",
                parameterName: null,
                parameterValue: null))
            .ToArray();

    /// <summary>The first column of every returned row, as text.</summary>
    private async Task<List<string>> ScalarsAsync(string sql, string? parameterName, string? parameterValue)
    {
        var parameters = new Dictionary<string, object?>();
        if (parameterName is not null && parameterValue is not null)
        {
            parameters[parameterName] = parameterValue;
        }

        var rows = await _helper!.ExecuteSqlQueryAsync(sql, parameters, MySqlDatabaseTarget.MtmWaitlist);

        return rows
            .Select(row => Convert.ToString(row.Values.First(), System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? string.Empty)
            .ToList();
    }
}
