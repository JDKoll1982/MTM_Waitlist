using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Setup.Services;

namespace MTM_Waitlist.Tests.Module_Setup.Services;

[TestClass]
public sealed class InforVisualSqlQueryServiceTests
{
    [TestMethod]
    public async Task LookupWorkOrderQuery_ReturnsRows_WhenServerIsAvailable()
    {
        await SkipIfServerUnavailableAsync();

        var service = CreateService();
        var rows = await service.ExecuteQueueAsync(
            "LookupWorkOrder",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = "WO-076951",
            });

        Assert.IsTrue(rows.Count > 0, "LookupWorkOrder should return at least one row when the Infor Visual server is available.");
        Assert.IsTrue(rows.Any(row => row.TryGetValue("PartNumber", out var partNumber) && !string.IsNullOrWhiteSpace(Convert.ToString(partNumber))), "LookupWorkOrder rows should include a part number.");
    }

    [TestMethod]
    public async Task GetSequencesQuery_ReturnsRows_WhenServerIsAvailable()
    {
        await SkipIfServerUnavailableAsync();

        var service = CreateService();
        var rows = await service.ExecuteQueueAsync(
            "GetSequences",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = "WO-076952",
                ["PartNumber"] = "12345679",
            });

        Assert.IsTrue(rows.Count > 0, "GetSequences should return at least one sequence when the Infor Visual server is available.");
        Assert.IsTrue(rows.Any(row => row.TryGetValue("SequenceNumber", out var sequenceNumber) && !string.IsNullOrWhiteSpace(Convert.ToString(sequenceNumber))), "GetSequences rows should include a sequence number.");
    }

    [TestMethod]
    public async Task GetSubordinatePartsQuery_ReturnsRows_WhenServerIsAvailable()
    {
        await SkipIfServerUnavailableAsync();

        var service = CreateService();
        var rows = await service.ExecuteQueueAsync(
            "GetSubordinateParts",
            new Dictionary<string, object?>
            {
                ["NormalizedWorkOrder"] = "WO-076952",
                ["PartNumber"] = "12345679",
                ["SequenceNumber"] = "20",
            });

        Assert.IsTrue(rows.Count > 0, "GetSubordinateParts should return rows when the Infor Visual server is available.");
        Assert.IsTrue(rows.Any(row => row.TryGetValue("PartNumber", out var partNumber) && !string.IsNullOrWhiteSpace(Convert.ToString(partNumber))), "GetSubordinateParts rows should include a subordinate part number.");
    }

    [TestMethod]
    public async Task EmployeeLookupQuery_ReturnsEmployeeStatus_WhenServerIsAvailable()
    {
        await SkipIfServerUnavailableAsync();

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Infor Visual SQL connection details are not configured; skipping employee verification test.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(
            @"SELECT employee.ID AS EmployeeNumber, employee.NAME AS EmployeeName, employee.ACTIVE AS IsActive
              FROM dbo.EMPLOYEE AS employee
              WHERE employee.ID = @EmployeeNumber;",
            connection);

        _ = command.Parameters.AddWithValue("@EmployeeNumber", "6229");

        await using var reader = await command.ExecuteReaderAsync();
        var foundRow = false;
        while (await reader.ReadAsync())
        {
            foundRow = true;
            Assert.AreEqual("6229", Convert.ToString(reader["EmployeeNumber"]));
            Assert.AreEqual("John Koll", Convert.ToString(reader["EmployeeName"]));
            Assert.AreEqual("Y", Convert.ToString(reader["IsActive"]));
        }

        Assert.IsTrue(foundRow, "The known active employee test record should exist in the Infor Visual EMPLOYEE table when the server is available.");
    }

    private static InforVisualSqlQueryService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InforVisualDatabaseOptions:Server"] = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_SERVER") ?? "VISUAL",
                ["InforVisualDatabaseOptions:Database"] = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_DATABASE") ?? "MTMFG",
                ["InforVisualDatabaseOptions:User"] = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_USER") ?? "SHOP2",
                ["InforVisualDatabaseOptions:Password"] = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_PASSWORD") ?? "SHOP",
                ["InforVisualDatabaseOptions:ConnectionTimeoutSeconds"] = "10",
            })
            .Build();

        return new InforVisualSqlQueryService(configuration);
    }

    private static async Task SkipIfServerUnavailableAsync()
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Infor Visual SQL connection settings are not configured; skipping integration test.");
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Infor Visual SQL Server is not available; skipping integration test. {ex.Message}");
        }
    }

    private static string ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_CONNECTION_STRING")?.Trim();
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var server = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_SERVER")?.Trim();
        var database = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_DATABASE")?.Trim();
        var user = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_USER")?.Trim();
        var password = Environment.GetEnvironmentVariable("INFOR_VISUAL_SQL_PASSWORD")?.Trim();

        if (string.IsNullOrWhiteSpace(server))
        {
            server = "VISUAL";
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            database = "MTMFG";
        }

        if (string.IsNullOrWhiteSpace(user))
        {
            user = "SHOP2";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            password = "SHOP";
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            UserID = user,
            Password = password,
            Encrypt = false,
            TrustServerCertificate = true,
            ConnectTimeout = 10,
        };

        return builder.ConnectionString;
    }
}
