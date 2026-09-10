using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Contracts;

/// <summary>
/// Reads a read shape's artifact metadata from the <c>mtm_mock</c> database.
/// </summary>
/// <remarks>
/// The only implementation routes through <c>sp_visual_read_shape_metadata_get</c>. Constitution III
/// forbids inline/hard-coded SQL statement text in application code, so the schema inspection must
/// live in a procedure rather than in this layer (data-model.md §12, tasks T109/T110).
/// </remarks>
public interface IVisualShapeMetadataReader
{
    /// <summary>
    /// Returns metadata for one shape, or <see langword="null"/> when the query returned no row.
    /// </summary>
    /// <param name="shapeKey">The read shape key to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<VisualShapeMetadata?> GetShapeMetadataAsync(string shapeKey, CancellationToken cancellationToken = default);
}
