using System.Data.Common;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// The narrowed seam's two guarantees a screen depends on: a read that answers more rows than the interface can
/// use is bounded rather than unbounded (US4 #4), and statement text supplied in place of a script name is
/// refused before anything is contacted (US4 #2).
/// </summary>
[TestClass]
public sealed class MySqlHelperServerRowCapTests
{
    /// <remarks>
    /// The cap is proved on the shared reader both routes go through, with a reader that would answer 25 rows
    /// more than the cap. The bounded list is what the caller gets, and it holds the first rows rather than an
    /// arbitrary slice.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerRowCapTests_AReadPastTheSharedRowCapIsBounded()
    {
        var reader = new FixedRowReader(MySqlHelperServer.MaxRowsPerRead + 25);

        var rows = await MySqlHelperServer.ReadRowsBoundedAsync(reader, CancellationToken.None);

        Assert.AreEqual(
            MySqlHelperServer.MaxRowsPerRead,
            rows.Count,
            "A read past the cap returns the cap, not every row the server offered.");
        Assert.AreEqual(0, Convert.ToInt32(rows[0]["Row"]), "The rows kept are the first ones read.");
        Assert.AreEqual(
            MySqlHelperServer.MaxRowsPerRead - 1,
            Convert.ToInt32(rows[^1]["Row"]),
            "The list ends exactly at the cap.");
    }

    /// <remarks>
    /// A caller that hands the reviewed route statement text instead of a script name gets nothing back, and
    /// the unreachable connection string proves nothing was contacted: had a connection been attempted, the
    /// store would have been recorded as unavailable.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerRowCapTests_StatementTextInPlaceOfAScriptNameIsRefused()
    {
        var tracker = new StoreAvailabilityTracker();
        var helper = new MySqlHelperServer(
            waitlistDatabaseOptions: Options.Create(
                new WaitlistDatabaseOptions { ConnectionString = "Server=127.0.0.1;Port=1;Database=mtm_waitlist;" }),
            storeAvailability: tracker,
            retryPolicy: new InternalStoreRetryPolicy(
                delay: (_, _) => Task.CompletedTask,
                maxAttempts: 1));

        var rows = await helper.ExecuteReviewedScriptQueryAsync(
            "SELECT PartNumber FROM inv_inventory",
            new Dictionary<string, object?>());

        Assert.IsNull(rows, "Statement text is not a reviewed script name, so the read is refused.");
        Assert.AreEqual(
            InternalStoreStatus.Unknown,
            tracker.Get(MySqlDatabaseTarget.MtmWipApplication).Status,
            "The refusal happens before a connection is opened, so no availability state is recorded.");
    }

    /// <summary>A reader that answers a fixed number of single-column rows, lazily.</summary>
    private sealed class FixedRowReader : DbDataReader
    {
        private readonly int _rowCount;
        private int _rowIndex = -1;

        public FixedRowReader(int rowCount) => _rowCount = rowCount;

        public override int FieldCount => 1;

        public override int Depth => 0;

        public override bool HasRows => _rowCount > 0;

        public override bool IsClosed => false;

        public override int RecordsAffected => -1;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override string GetName(int ordinal) => "Row";

        public override int GetOrdinal(string name) => 0;

        public override object GetValue(int ordinal) => _rowIndex;

        public override int GetValues(object[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            values[0] = _rowIndex;
            return 1;
        }

        public override bool IsDBNull(int ordinal) => false;

        public override bool Read() => ReadAsync(CancellationToken.None).GetAwaiter().GetResult();

        public override bool NextResult() => false;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(++_rowIndex < _rowCount);

        public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();

        public override bool GetBoolean(int ordinal) => throw new NotSupportedException();

        public override byte GetByte(int ordinal) => throw new NotSupportedException();

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override char GetChar(int ordinal) => throw new NotSupportedException();

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override string GetDataTypeName(int ordinal) => "INT";

        public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();

        public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();

        public override double GetDouble(int ordinal) => throw new NotSupportedException();

        public override Type GetFieldType(int ordinal) => typeof(int);

        public override float GetFloat(int ordinal) => throw new NotSupportedException();

        public override Guid GetGuid(int ordinal) => throw new NotSupportedException();

        public override short GetInt16(int ordinal) => throw new NotSupportedException();

        public override int GetInt32(int ordinal) => throw new NotSupportedException();

        public override long GetInt64(int ordinal) => throw new NotSupportedException();

        public override string GetString(int ordinal) => throw new NotSupportedException();
    }
}
