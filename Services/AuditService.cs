using System;
using System.Threading.Tasks;
using TrendyKart.Data;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(string adminEmail, string action, string entityName, string? entityId = null, string? details = null, string? ipAddress = null)
        {
            try
            {
                var log = new AuditLog
                {
                    AdminEmail = adminEmail,
                    Action = action,
                    EntityName = entityName,
                    EntityID = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silently prevent logging failure from breaking core operations
            }
        }
    }
}
