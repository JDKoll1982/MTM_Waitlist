namespace MTM_Waitlist.Module_DevTools.Models;

public sealed class RequestTypeFieldDefinition
{
    public RequestTypeFieldDefinition(string fieldName, RequestFieldDataType dataType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        FieldName = fieldName.Trim();
        DataType = dataType;
    }

    public string FieldName { get; }

    public RequestFieldDataType DataType { get; }

    public string DataTypeLabel => DataType.ToDisplayLabel();
}
