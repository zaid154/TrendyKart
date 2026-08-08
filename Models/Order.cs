using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendyKart.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCharge { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "Razorpay";

        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }

        [StringLength(100)]
        public string? RazorpayPaymentId { get; set; }

        [StringLength(200)]
        public string? RazorpaySignature { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Country { get; set; } = "India";
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public Payment? Payment { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderFeedback>? OrderFeedbacks { get; set; }
    }
}