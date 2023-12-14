using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    public partial class _20231121_modify : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Senparc_PromptRange_PromptResult_Senparc_PromptRange_LlmModel_LlmModelId",
                table: "Senparc_PromptRange_PromptResult");

            migrationBuilder.DropIndex(
                name: "IX_Senparc_PromptRange_PromptResult_LlmModelId",
                table: "Senparc_PromptRange_PromptResult");

            migrationBuilder.DropColumn(
                name: "OtherModelName",
                table: "Senparc_PromptRange_LlmModel");

            migrationBuilder.DropColumn(
                name: "TextCompletionModelName",
                table: "Senparc_PromptRange_LlmModel");

            migrationBuilder.DropColumn(
                name: "TextEmbeddingModelName",
                table: "Senparc_PromptRange_LlmModel");

            migrationBuilder.AlterColumn<int>(
                name: "MaxToken",
                table: "Senparc_PromptRange_PromptItem",
                type: "int",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<string>(
                name: "ModelType",
                table: "Senparc_PromptRange_LlmModel",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MaxToken",
                table: "Senparc_PromptRange_PromptItem",
                type: "real",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ModelType",
                table: "Senparc_PromptRange_LlmModel",
                type: "int",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "OtherModelName",
                table: "Senparc_PromptRange_LlmModel",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextCompletionModelName",
                table: "Senparc_PromptRange_LlmModel",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEmbeddingModelName",
                table: "Senparc_PromptRange_LlmModel",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_PromptRange_PromptResult_LlmModelId",
                table: "Senparc_PromptRange_PromptResult",
                column: "LlmModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Senparc_PromptRange_PromptResult_Senparc_PromptRange_LlmModel_LlmModelId",
                table: "Senparc_PromptRange_PromptResult",
                column: "LlmModelId",
                principalTable: "Senparc_PromptRange_LlmModel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
