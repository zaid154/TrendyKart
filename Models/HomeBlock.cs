using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TrendyKart.Models
{
    // One flexible entity for every editable region of the homepage.
    // The "Section" tells the homepage where the block belongs.
    public class HomeBlock
    {
        [Key]
        public int Id { get; set; }

        // Which part of the homepage this block belongs to:
        // "Hero" | "FeatureTile" | "CategoryChip" | "PopularTile" | "SaleBanner"
        public string Section { get; set; } = string.Empty;

        // Optional stable key to find a single block (e.g. "hero", "summer-sale").
        public string? Slug { get; set; }

        // Text fields (not every block uses all of them).
        public string? Eyebrow { get; set; }      // small text above the title (hero)
        public string? Title { get; set; }
        public string? Subtitle { get; set; }      // for CategoryChip this holds the Font Awesome icon class, e.g. "fa-mobile-screen"
        public string? ButtonText { get; set; }
        public string? LinkUrl { get; set; }       // where the button/tile links to

        // Uploaded image path like "/images/xxxx.jpg". Empty => a placeholder box is shown.
        public string ImageUrl { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Layout hints used by the feature (bento) tiles.
        public string? Size { get; set; }          // "Large" | "Small"
        public string? Theme { get; set; }         // "Light" | "Gray" | "Dark"

        // The uploaded file from the admin form (not saved to the database).
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
