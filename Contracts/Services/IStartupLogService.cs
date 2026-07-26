namespace MTM_Waitlist.Contracts.Services;

public interface IStartupLogService
{
    void Info(string area, string message);

    void Error(string area, Exception? exception, string message);
}
