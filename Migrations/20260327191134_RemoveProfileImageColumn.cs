using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrendyKart.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProfileImageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Admins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
