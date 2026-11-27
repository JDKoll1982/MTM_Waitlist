using System.Text.Json.Serialization;

namespace MTM_Waitlist.Module_Core.Helpers;

/// <summary>
/// Generated metadata for the values this assembly stores as text.
/// </summary>
/// <remarks>
/// A store that persists a value passes the matching <c>JsonTypeInfo</c> from here rather than relying on the
/// reflection serializer, so the shape it writes and reads is fixed at build time.
/// </remarks>
[JsonSerializable(typeof(Dictionary<string, DateTimeOffset>))]
public sealed partial class CoreJsonContext : JsonSerializerContext;
