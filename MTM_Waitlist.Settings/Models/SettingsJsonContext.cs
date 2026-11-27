using System.Text.Json.Serialization;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The generated serializer metadata for every value the settings library stores.
/// </summary>
/// <remarks>
/// Generated rather than reflected: a store that serializes by reflection pays a start-up cost for metadata it
/// could have been told at build time, and it cannot be trimmed. Every stored shape is listed here, so a new one
/// that is not listed fails on its first write instead of quietly falling back to reflection.
/// </remarks>
[JsonSerializable(typeof(UserListFilter.Payload))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
