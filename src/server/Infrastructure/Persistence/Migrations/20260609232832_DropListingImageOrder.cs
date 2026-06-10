using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharePrint.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropListingImageOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListingImages_ListingId_Order",
                table: "ListingImages");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "ListingImages");

            migrationBuilder.CreateIndex(
                name: "IX_ListingImages_ListingId_CreatedAt",
                table: "ListingImages",
                columns: new[] { "ListingId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListingImages_ListingId_CreatedAt",
                table: "ListingImages");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "ListingImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ListingImages_ListingId_Order",
                table: "ListingImages",
                columns: new[] { "ListingId", "Order" });
        }
    }
}
