using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
{
    public partial class Add_AdminChatWorkspace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "ADMIN_AdminChatSession",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "WorkspaceWriteEnabled",
                table: "ADMIN_AdminChatSession",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WorkspaceCliEnabled",
                table: "ADMIN_AdminChatSession",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("WorkspaceCliEnabled", "ADMIN_AdminChatSession");
            migrationBuilder.DropColumn("WorkspaceWriteEnabled", "ADMIN_AdminChatSession");
            migrationBuilder.DropColumn("WorkspaceId", "ADMIN_AdminChatSession");
        }
    }
}
