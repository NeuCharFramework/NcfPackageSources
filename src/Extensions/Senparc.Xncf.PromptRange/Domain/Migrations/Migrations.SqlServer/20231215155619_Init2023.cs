using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Init2023 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_LlmModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ModelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxToken = table.Column<int>(type: "int", nullable: false),
                    Show = table.Column<bool>(type: "bit", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_LlmModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    TopP = table.Column<float>(type: "real", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    MaxToken = table.Column<int>(type: "int", nullable: false),
                    FrequencyPenalty = table.Column<float>(type: "real", nullable: false),
                    PresencePenalty = table.Column<float>(type: "real", nullable: false),
                    StopSequences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumsOfResults = table.Column<int>(type: "int", nullable: false),
                    ChatSystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenSelectionBiases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluationScore = table.Column<int>(type: "int", nullable: false),
                    FullVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tactic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aiming = table.Column<int>(type: "int", nullable: false),
                    ParentTac = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsShare = table.Column<bool>(type: "bit", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LlmModelId = table.Column<int>(type: "int", nullable: false),
                    ResultString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostTime = table.Column<double>(type: "float", nullable: false),
                    RobotScore = table.Column<int>(type: "int", nullable: false),
                    HumanScore = table.Column<int>(type: "int", nullable: false),
                    RobotTestExceptedResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRobotTestExactlyEquat = table.Column<bool>(type: "bit", nullable: false),
                    TestType = table.Column<int>(type: "int", nullable: false),
                    PromptCostToken = table.Column<int>(type: "int", nullable: false),
                    ResultCostToken = table.Column<int>(type: "int", nullable: false),
                    TotalCostToken = table.Column<int>(type: "int", nullable: false),
                    PromptItemId = table.Column<int>(type: "int", nullable: false),
                    PromptItemVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptResult", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_LlmModel");

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptResult");
        }
    }
}
