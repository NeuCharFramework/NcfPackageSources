using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.SqlServer
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
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalRequestId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolCallId",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalToolName",
                table: "ADMIN_WeixinClawAdminBinding",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
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
