namespace MTM_Waitlist.Mock.Models;

/// <summary>
/// One input parameter of an Infor Visual read shape.
/// </summary>
/// <param name="Name">Parameter name as it appears in the source script and the mirror's key column.</param>
/// <param name="Type">Source parameter type (for example <c>nvarchar(30)</c>).</param>
/// <param name="IsRequired">Whether the read requires a value for this parameter.</param>
public sealed record VisualShapeParameter(string Name, string Type, bool IsRequired);
