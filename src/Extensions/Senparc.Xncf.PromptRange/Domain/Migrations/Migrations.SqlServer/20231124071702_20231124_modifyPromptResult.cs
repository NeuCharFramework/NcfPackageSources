using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    public partial class _20231124_modifyPromptResult : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptGroupId",
                table: "Senparc_PromptRange_PromptResult");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromptGroupId",
                table: "Senparc_PromptRange_PromptResult",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
