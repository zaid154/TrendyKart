using System.Threading.Tasks;

namespace TrendyKart.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string adminEmail, string action, string entityName, string? entityId = null, string? details = null, string? ipAddress = null);
    }
}
