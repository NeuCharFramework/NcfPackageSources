using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UpdateNewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminRemark",
                table: "WeixinManager_UserTag_WeixinUser",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "WeixinManager_UserTag_WeixinUser",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminRemark",
                table: "WeixinManager_UserTag_WeixinUser");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "WeixinManager_UserTag_WeixinUser");
        }
    }
}
