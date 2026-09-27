using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Sqlite
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
                type: "TEXT",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageFromUserId",
                table: "WeixinManager_WeixinClawAccount",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
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
