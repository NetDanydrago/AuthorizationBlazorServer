using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthorizationBlazorServer.Server.Data.Migrations.User
{
    public partial class codevalidation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValidationCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationCode",
                table: "Users");
        }
    }
}
