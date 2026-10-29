using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToCachedObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Workspace",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Report",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Dataset",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Capacity",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Workspace");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Dataset");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Capacity");
        }
    }
}
