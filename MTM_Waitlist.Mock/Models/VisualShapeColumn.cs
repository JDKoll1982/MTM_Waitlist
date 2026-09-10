namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// One output column of an Infor Visual read shape.
/// </summary>
/// <param name="Name">Result column name as the live read projects it (PascalCase).</param>
/// <param name="Type">Result column type.</param>
public sealed record VisualShapeColumn(string Name, string Type);
