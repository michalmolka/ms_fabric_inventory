using System;
using blz0.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    [DbContext(typeof(blz0Context))]
    [Migration("20260531120000_AddDatasetRefreshCacheColumns")]
    public partial class AddDatasetRefreshCacheColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshAtUtc",
                table: "Dataset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshCacheUpdatedAtUtc",
                table: "Dataset",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastRefreshStatus",
                table: "Dataset",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRefreshAtUtc",
                table: "Dataset");

            migrationBuilder.DropColumn(
                name: "LastRefreshCacheUpdatedAtUtc",
                table: "Dataset");

            migrationBuilder.DropColumn(
                name: "LastRefreshStatus",
                table: "Dataset");
        }
    }
}
