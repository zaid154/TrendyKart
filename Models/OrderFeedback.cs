using System;
using System.Collections.Generic;
namespace TrendyKart.Models
{
    public class OrderFeedback
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsReadByAdmin { get; set; } = false;
        public ICollection<FeedbackMessage>? Messages { get; set; }
    }
}