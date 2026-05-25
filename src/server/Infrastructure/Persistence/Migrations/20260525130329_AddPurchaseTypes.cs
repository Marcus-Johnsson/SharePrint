using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharePrint.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DownloadAble",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintAble",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadAble",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PrintAble",
                table: "Listings");
        }
    }
}
