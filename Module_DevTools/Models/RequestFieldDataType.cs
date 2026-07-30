namespace MTM_Waitlist.Module_DevTools.Models;

public enum RequestFieldDataType
{
    String,
    Int,
    Boolean,
    List,
    VisualSqlDatabaseQueue,
    MySqlMtmWaitlistQueue
}

internal static class RequestFieldDataTypeExtensions
{
    public static string ToDisplayLabel(this RequestFieldDataType dataType)
    {
        return dataType switch
        {
            RequestFieldDataType.String => "String",
            RequestFieldDataType.Int => "Int",
            RequestFieldDataType.Boolean => "Boolean",
            RequestFieldDataType.List => "List",
            RequestFieldDataType.VisualSqlDatabaseQueue => "Visual SQL Database Queue",
            RequestFieldDataType.MySqlMtmWaitlistQueue => "MySQL mtm_waitlist Queue",
            _ => "String"
        };
    }

    public static string ToDatabaseValue(this RequestFieldDataType dataType)
    {
        return dataType.ToDisplayLabel();
    }
}
