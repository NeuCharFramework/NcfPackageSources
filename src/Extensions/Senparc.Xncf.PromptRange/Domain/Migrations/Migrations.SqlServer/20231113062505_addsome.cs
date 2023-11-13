using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    public partial class addsome : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptGroup");

            migrationBuilder.DropColumn(
                name: "PromptGroupId",
                table: "Senparc_PromptRange_PromptItem");

            migrationBuilder.RenameColumn(
                name: "ResultsPerPrompt",
                table: "Senparc_PromptRange_PromptItem",
                newName: "NumsOfResults");

            migrationBuilder.AddColumn<string>(
                name: "ModelType",
                table: "Senparc_PromptRange_LlmModel",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelType",
                table: "Senparc_PromptRange_LlmModel");

            migrationBuilder.RenameColumn(
                name: "NumsOfResults",
                table: "Senparc_PromptRange_PromptItem",
                newName: "ResultsPerPrompt");

            migrationBuilder.AddColumn<int>(
                name: "PromptGroupId",
                table: "Senparc_PromptRange_PromptItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptGroup", x => x.Id);
                });
        }
    }
}
