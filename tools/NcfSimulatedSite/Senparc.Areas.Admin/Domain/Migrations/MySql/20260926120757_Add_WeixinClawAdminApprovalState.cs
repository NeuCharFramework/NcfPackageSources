using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_WeixinClawAdminApprovalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalArgumentsJson",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalRequestId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolCallId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolName",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingApprovalArgumentsJson",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalRequestId",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalToolCallId",
                table: "ADMIN_WeixinClawAdminBinding");

            migrationBuilder.DropColumn(
                name: "PendingApprovalToolName",
                table: "ADMIN_WeixinClawAdminBinding");
        }
    }
}
