using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using blz0.Data;

#nullable disable

namespace blz0.Migrations
{
    [DbContext(typeof(blz0Context))]
    [Migration("20260602090000_AddUserObjectIdToInventoryTables")]
    public partial class AddUserObjectIdToInventoryTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Capacity",
                table: "Capacity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Dataset",
                table: "Dataset");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Report",
                table: "Report");

            migrationBuilder.AddColumn<string>(
                name: "UserObjectId",
                table: "Capacity",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserObjectId",
                table: "Workspace",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserObjectId",
                table: "Dataset",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserObjectId",
                table: "Report",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ObjectType",
                table: "CacheRefreshState",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ScopeId",
                table: "CacheRefreshState",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserObjectId",
                table: "CacheRefreshState",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Capacity",
                table: "Capacity",
                columns: new[] { "UserObjectId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace",
                columns: new[] { "UserObjectId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dataset",
                table: "Dataset",
                columns: new[] { "UserObjectId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Report",
                table: "Report",
                columns: new[] { "UserObjectId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CacheRefreshState_UserObjectId_ObjectType_ScopeId",
                table: "CacheRefreshState",
                columns: new[] { "UserObjectId", "ObjectType", "ScopeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CacheRefreshState_UserObjectId_ObjectType_ScopeId",
                table: "CacheRefreshState");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Report",
                table: "Report");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Dataset",
                table: "Dataset");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Capacity",
                table: "Capacity");

            migrationBuilder.DropColumn(
                name: "UserObjectId",
                table: "CacheRefreshState");

            migrationBuilder.DropColumn(
                name: "UserObjectId",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "UserObjectId",
                table: "Dataset");

            migrationBuilder.DropColumn(
                name: "UserObjectId",
                table: "Workspace");

            migrationBuilder.DropColumn(
                name: "UserObjectId",
                table: "Capacity");

            migrationBuilder.AlterColumn<string>(
                name: "ScopeId",
                table: "CacheRefreshState",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ObjectType",
                table: "CacheRefreshState",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Report",
                table: "Report",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dataset",
                table: "Dataset",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspace",
                table: "Workspace",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Capacity",
                table: "Capacity",
                column: "Id");
        }
    }
}
