using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    /// <inheritdoc />
    public partial class Migration03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "reportType",
                table: "Report",
                newName: "ReportType");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Report",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "isFromPbix",
                table: "Report",
                newName: "IsFromPbix");

            migrationBuilder.RenameColumn(
                name: "datasetWorkspaceId",
                table: "Report",
                newName: "DatasetWorkspaceId");

            migrationBuilder.RenameColumn(
                name: "datasetId",
                table: "Report",
                newName: "DatasetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReportType",
                table: "Report",
                newName: "reportType");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Report",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "IsFromPbix",
                table: "Report",
                newName: "isFromPbix");

            migrationBuilder.RenameColumn(
                name: "DatasetWorkspaceId",
                table: "Report",
                newName: "datasetWorkspaceId");

            migrationBuilder.RenameColumn(
                name: "DatasetId",
                table: "Report",
                newName: "datasetId");
        }
    }
}
