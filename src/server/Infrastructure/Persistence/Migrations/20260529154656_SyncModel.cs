using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharePrint.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DownloadPath",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrintPath",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadPath",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PrintPath",
                table: "OrderItems");
        }
    }
}
