using System.Collections.Generic;

namespace TrendyKart.Models
{
    // Everything the homepage needs, built once in HomeController.Index.
    public class HomeViewModel
    {
        // Editable content blocks (from the HomeBlocks table).
        public HomeBlock? Hero { get; set; }
        public List<HomeBlock> FeatureTiles { get; set; } = new();
        public List<HomeBlock> CategoryChips { get; set; } = new();
        public List<HomeBlock> PopularTiles { get; set; } = new();
        public HomeBlock? SaleBanner { get; set; }

        // Product grids (from the Products table).
        public List<Product> NewArrivals { get; set; } = new();
        public List<Product> Bestsellers { get; set; } = new();
        public List<Product> Featured { get; set; } = new();
        public List<Product> Discounted { get; set; } = new();
    }
}
