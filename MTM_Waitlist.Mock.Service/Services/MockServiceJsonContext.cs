using System.Text.Json;
using System.Text.Json.Serialization;

using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The generated serializer metadata for every value the mock service writes to disk or hands back.
/// </summary>
/// <remarks>
/// Generated rather than reflected, so a shape's metadata is built when this library is, not on the first write
/// that happens to need it. Every stored shape is listed: one that is missing fails on its first write rather
/// than quietly reverting to reflection.
/// </remarks>
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(RunHistoryEntry))]
[JsonSerializable(typeof(List<RefreshRunRecord>))]
[JsonSerializable(typeof(BackupArtifactStore.PersistedState))]
[JsonSerializable(typeof(ServiceConfigurationStore.ServiceConfigurationFile))]
[JsonSerializable(typeof(ServiceLog.ServiceLogEntry))]
internal sealed partial class MockServiceJsonContext : JsonSerializerContext
{
}

/// <summary>
/// The one place the mock service defines how a stored value is serialized. Each store names the options that
/// describe its own file format; none of them builds options at call time.
/// </summary>
internal static class MockServiceJson
{
    /// <summary>A file an operator may open: indented, with enums by name and absent rather than null values.</summary>
    public static readonly JsonSerializerOptions StoredFile = new(MockServiceJsonContext.Default.Options)
    {
        WriteIndented = true,
    };

    /// <summary>One line per run, read by the reliability measurement script.</summary>
    public static readonly JsonSerializerOptions RunHistoryLine = new(MockServiceJsonContext.Default.Options);

    /// <summary>A log line, whose fields are fixed so a line's shape never varies.</summary>
    public static readonly JsonSerializerOptions LogLine = new(MockServiceJsonContext.Default.Options);
}
