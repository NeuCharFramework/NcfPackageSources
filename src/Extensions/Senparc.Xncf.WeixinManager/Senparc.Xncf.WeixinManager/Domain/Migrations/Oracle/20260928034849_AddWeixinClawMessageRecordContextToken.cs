using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageRecordContextToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextTokenProtected",
                table: "WeixinManager_WeixinClawMessageRecord",
                type: "NCLOB",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextTokenProtected",
                table: "WeixinManager_WeixinClawMessageRecord");
        }
    }
}
