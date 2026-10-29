using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    /// <inheritdoc />
    public partial class AddLakehousesAndWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lakehouse",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserObjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OneLakeTablesPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OneLakeFilesPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SqlEndpointId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SqlEndpointConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SqlEndpointProvisioningStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lakehouse", x => new { x.UserObjectId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserObjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsertDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => new { x.UserObjectId, x.Id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lakehouse");

            migrationBuilder.DropTable(
                name: "Warehouse");
        }
    }
}
