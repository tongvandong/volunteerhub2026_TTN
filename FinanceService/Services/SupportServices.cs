using System.Threading.Tasks;

namespace FinanceService.Services
{
    public interface IAuditLogService
    {
        Task RecordAsync(int? userId, string action, string entityType, int? entityId = null, string? metadata = null, string? ipAddress = null);
    }

    public interface INotificationService
    {
        Task SendAsync(int userId, string title, string message, string type, int? entityId = null);
    }

    public interface IBadgeService
    {
        Task CheckAndAwardAsync(int userId);
    }

    public class AuditLogService : IAuditLogService
    {
        public Task RecordAsync(int? userId, string action, string entityType, int? entityId = null, string? metadata = null, string? ipAddress = null)
        {
            // Stubbed for microservice separation
            return Task.CompletedTask;
        }
    }

    public class NotificationService : INotificationService
    {
        public Task SendAsync(int userId, string title, string message, string type, int? entityId = null)
        {
            return Task.CompletedTask;
        }
    }

    public class BadgeService : IBadgeService
    {
        public Task CheckAndAwardAsync(int userId)
        {
            return Task.CompletedTask;
        }
    }
}
