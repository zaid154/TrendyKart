using System;
using System.ComponentModel.DataAnnotations;

namespace TrendyKart.Models
{
    public class AuditLog
    {
        [Key]
        public int LogID { get; set; }

        [Required, StringLength(100)]
        public string AdminEmail { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public string? EntityID { get; set; }

        public string? Details { get; set; }

        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
