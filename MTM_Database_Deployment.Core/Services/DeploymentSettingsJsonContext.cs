using System.Text.Json.Serialization;

using MTM_Database_Deployment.Models;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Generated metadata for <c>deployment.settings.json</c>, so the file this tool writes and reads has a shape fixed
/// at build time rather than one discovered by reflection.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Model_ConnectionSettings))]
public sealed partial class DeploymentSettingsJsonContext : JsonSerializerContext;
