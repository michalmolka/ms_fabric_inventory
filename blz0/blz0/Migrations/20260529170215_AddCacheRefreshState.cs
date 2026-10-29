using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    /// <inheritdoc />
    public partial class AddCacheRefreshState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CacheRefreshState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheRefreshState", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CacheRefreshState_CacheKey",
                table: "CacheRefreshState",
                column: "CacheKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CacheRefreshState");
        }
    }
}
