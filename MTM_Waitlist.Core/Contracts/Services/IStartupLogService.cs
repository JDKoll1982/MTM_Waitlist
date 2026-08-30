namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupLogService
{
    void Info(string area, string message);

    void Error(string area, Exception? exception, string message);
}
