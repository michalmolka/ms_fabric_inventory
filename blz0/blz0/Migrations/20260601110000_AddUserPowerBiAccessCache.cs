using System;
using blz0.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blz0.Migrations
{
    [DbContext(typeof(blz0Context))]
    [Migration("20260601110000_AddUserPowerBiAccessCache")]
    public partial class AddUserPowerBiAccessCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPowerBiAccessCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPowerBiAccessCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPowerBiAccessCache_UserKey_ObjectType_ObjectId_ScopeId",
                table: "UserPowerBiAccessCache",
                columns: new[] { "UserKey", "ObjectType", "ObjectId", "ScopeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPowerBiAccessCache");
        }
    }
}
