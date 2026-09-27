using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastMessageContextTokenProtected",
                table: "WeixinManager_WeixinClawAccount",
                type: "varchar(4096)",
                maxLength: 4096,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessageContextTokenProtected",
                table: "WeixinManager_WeixinClawAccount");

            migrationBuilder.DropColumn(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount");
        }
    }
}
