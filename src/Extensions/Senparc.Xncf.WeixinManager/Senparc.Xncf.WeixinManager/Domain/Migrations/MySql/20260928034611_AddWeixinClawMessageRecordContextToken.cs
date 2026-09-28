using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.MySql
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
                type: "varchar(4096)",
                maxLength: 4096,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
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
