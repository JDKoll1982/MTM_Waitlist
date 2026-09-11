using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;
using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock_Service;

/// <summary>
/// Test double for <see cref="IVisualShapeMetadataReader"/> that reports the shipped catalog's artifacts as
/// present and correctly shaped, so catalog validation succeeds without a database.
/// </summary>
/// <remarks>
/// The column list it reports is derived from each shape's own inputs and outputs, mirroring the physical
/// mirror layout the real procedure describes, so a shape added to the catalog is covered automatically.
/// </remarks>
public sealed class AcceptingMetadataReader : IVisualShapeMetadataReader
{
    public Task<VisualShapeMetadata?> GetShapeMetadataAsync(
        string shapeKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<VisualShapeMetadata?>(Metadata(shapeKey));

    public Task<IReadOnlyList<VisualShapeMetadata>> GetAllShapeMetadataAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VisualShapeMetadata>>(
            VisualReadShapeCatalog.Create().Select(shape => Metadata(shape.Key)).ToList());

    private static VisualShapeMetadata Metadata(string shapeKey)
    {
        var shape = VisualReadShapeCatalog.FindByKey(shapeKey);

        if (shape is null)
        {
            return new VisualShapeMetadata(shapeKey, 0, 0, 0, 0, null);
        }

        var columns = new List<string> { "id" };
        columns.AddRange(shape.InputParameters.Select(parameter => ToSnakeCase(parameter.Name)));
        columns.AddRange(shape.OutputColumns.Select(column => ToSnakeCase(column.Name)));
        columns.Add("refreshed_utc");
        columns.Add("is_seed_content");

        return new VisualShapeMetadata(
            shapeKey,
            1,
            1,
            1,
            1,
            string.Join(",", columns.Distinct(StringComparer.OrdinalIgnoreCase)));
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsUpper(character))
            {
                if (index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
