using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Pending";
        public DateTime? PaymentDate { get; set; }

        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }

        [StringLength(100)]
        public string? RazorpayPaymentId { get; set; }

        [StringLength(200)]
        public string? RazorpaySignature { get; set; }
    }
}