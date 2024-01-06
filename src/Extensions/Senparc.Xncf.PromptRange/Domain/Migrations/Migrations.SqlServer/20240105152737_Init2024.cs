using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Init2024 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_LlModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeploymentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ModelType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaxToken = table.Column<int>(type: "int", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Senparc_PromptRange_LlModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RangeId = table.Column<int>(type: "int", nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    TopP = table.Column<float>(type: "real", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    MaxToken = table.Column<int>(type: "int", nullable: false),
                    FrequencyPenalty = table.Column<float>(type: "real", nullable: false),
                    PresencePenalty = table.Column<float>(type: "real", nullable: false),
                    StopSequences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvalAvgScore = table.Column<int>(type: "int", maxLength: 3, nullable: false),
                    EvalMaxScore = table.Column<int>(type: "int", maxLength: 3, nullable: false),
                    ExpectedResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RangeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Tactic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Aiming = table.Column<int>(type: "int", maxLength: 5, nullable: false),
                    ParentTac = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Suffix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VariableDictJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsShare = table.Column<bool>(type: "bit", nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
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
                name: "Senparc_PromptRange_PromptRange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RangeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptRange", x => x.Id);
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
                    FinalScore = table.Column<int>(type: "int", nullable: false),
                    TestType = table.Column<int>(type: "int", nullable: false),
                    PromptCostToken = table.Column<int>(type: "int", nullable: false),
                    ResultCostToken = table.Column<int>(type: "int", nullable: false),
                    TotalCostToken = table.Column<int>(type: "int", nullable: false),
                    PromptItemId = table.Column<int>(type: "int", nullable: false),
                    PromptItemVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                name: "Senparc_PromptRange_LlModel");

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptItem");

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptRange");

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptResult");
        }
    }
}
