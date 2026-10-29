using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using blz0.Data;

#nullable disable

namespace blz0.Migrations
{
    [DbContext(typeof(blz0Context))]
    [Migration("20260602100000_AddUserObjectMap")]
    public partial class AddUserObjectMap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserObjectMap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserObjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserObjectMap", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserObjectMap_UserKey",
                table: "UserObjectMap",
                column: "UserKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserObjectMap_UserObjectId",
                table: "UserObjectMap",
                column: "UserObjectId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserObjectMap");
        }
    }
}
