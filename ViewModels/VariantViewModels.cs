using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace TrendyKart.ViewModels
{
    public class VariantDetailJsonDTO
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string FormattedPrice { get; set; } = string.Empty;
        public string? FormattedOldPrice { get; set; }
        public int SavingsPercentage { get; set; }
        public decimal SavingsAmount { get; set; }
        public int Stock { get; set; }
        public bool InStock { get; set; }
        public bool LowStock { get; set; }
        public string StockStatusText { get; set; } = string.Empty;
        public string? ColorName { get; set; }
        public string? ColorHex { get; set; }
        public string? Storage { get; set; }
        public string? RAM { get; set; }
        public string? Processor { get; set; }
        public string? ModelNumber { get; set; }
        public string? Warranty { get; set; }
        public string? WeightText { get; set; }
        public string? DimensionsText { get; set; }
        public string? Description { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public Dictionary<string, string> Specifications { get; set; } = new Dictionary<string, string>();
    }

    public class VariantFormDTO
    {
        public int VariantID { get; set; }
        public int ProductID { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int Stock { get; set; } = 10;
        public string? ColorName { get; set; }
        public string? ColorHex { get; set; }
        public string? Storage { get; set; }
        public string? RAM { get; set; }
        public string? Processor { get; set; }
        public string? ModelNumber { get; set; }
        public string? Warranty { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string? SpecificationsRaw { get; set; } // Key:Value per line
        public List<IFormFile>? ImageFiles { get; set; }
    }

    public class BulkVariantUpdateDTO
    {
        public List<int> VariantIds { get; set; } = new List<int>();
        public string ActionType { get; set; } = string.Empty; // "price", "stock", "activate", "deactivate", "delete"
        public decimal? PriceValue { get; set; }
        public int? StockValue { get; set; }
    }
}
