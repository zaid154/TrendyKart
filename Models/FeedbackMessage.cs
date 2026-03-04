using System;
namespace TrendyKart.Models
{
    public class FeedbackMessage
    {
        public int Id { get; set; }
        public int FeedbackId { get; set; }
        public OrderFeedback? Feedback { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}