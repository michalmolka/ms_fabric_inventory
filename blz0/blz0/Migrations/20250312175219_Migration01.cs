using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    /// <inheritdoc />
    public partial class Migration01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capacity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacityUserAccessRight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capacity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dataset",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddRowsAPIEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConfiguredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRefreshable = table.Column<bool>(type: "bit", nullable: false),
                    IsEffectiveIdentityRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsEffectiveIdentityRolesRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsOnPremGatewayRequired = table.Column<bool>(type: "bit", nullable: false),
                    TargetStorageMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateReportEmbedURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QnaEmbedURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpstreamDatasets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Users = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkspaceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dataset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueryScaleOutSettings",
                columns: table => new
                {
                    AutoSyncReadOnlyReplicas = table.Column<bool>(type: "bit", nullable: false),
                    MaxReadOnlyReplicas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Report",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    reportType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isFromPbix = table.Column<bool>(type: "bit", nullable: true),
                    datasetId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    datasetWorkspaceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkspaceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Report", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspace",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: true),
                    IsOnDedicatedCapacity = table.Column<bool>(type: "bit", nullable: true),
                    CapacityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultDatasetStorageFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspace", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capacity");

            migrationBuilder.DropTable(
                name: "Dataset");

            migrationBuilder.DropTable(
                name: "QueryScaleOutSettings");

            migrationBuilder.DropTable(
                name: "Report");

            migrationBuilder.DropTable(
                name: "Workspace");
        }
    }
}
