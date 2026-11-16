using System.Text;

namespace MTM_Database_Deployment.Services;

/// <summary>
/// Writes a crash to a file the operator can send to IT. A debugger that stops at a first-chance
/// exception leaves no trace for the person running the tool, so every unhandled failure leaves one.
/// </summary>
public static class Service_FailureLog
{
    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MTM Database Tool",
        "logs");

    /// <summary>
    /// The text that ends up in the log file. Pure so it can be asserted without touching disk.
    /// Never include the password: only the server, the database, and the exception are recorded.
    /// </summary>
    public static string Describe(string source, Exception exception, string? context = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MTM database tool - failure");
        builder.AppendLine($"When: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Where: {source}");
        builder.AppendLine($"Executable: {AppContext.BaseDirectory}");

        if (!string.IsNullOrWhiteSpace(context))
        {
            builder.AppendLine(context);
        }

        builder.AppendLine(new string('-', 80));

        var depth = 0;
        for (Exception? current = exception; current is not null && depth < 6; current = current.InnerException, depth++)
        {
            builder.AppendLine($"[{current.GetType().FullName}] {current.Message}");
            if (!string.IsNullOrWhiteSpace(current.StackTrace))
            {
                builder.AppendLine(current.StackTrace);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes the failure and returns the path, or null when even that fails.
    /// </summary>
    public static string? Write(string source, Exception exception, string? context = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, $"failure-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(path, Describe(source, exception, context));
            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
