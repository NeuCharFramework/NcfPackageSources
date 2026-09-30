using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageRecordRunId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RunId",
                table: "WeixinManager_WeixinClawMessageRecord",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RunId",
                table: "WeixinManager_WeixinClawMessageRecord");
        }
    }
}
