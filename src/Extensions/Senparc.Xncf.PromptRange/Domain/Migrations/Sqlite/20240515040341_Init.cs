using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_LlModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DeploymentName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ModelType = table.Column<int>(type: "INTEGER", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MaxToken = table.Column<int>(type: "INTEGER", nullable: false),
                    IsShared = table.Column<bool>(type: "INTEGER", nullable: false),
                    Show = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_LlModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RangeId = table.Column<int>(type: "INTEGER", nullable: false),
                    NickName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    ModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    TopP = table.Column<float>(type: "REAL", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", nullable: false),
                    MaxToken = table.Column<int>(type: "INTEGER", nullable: false),
                    FrequencyPenalty = table.Column<float>(type: "REAL", nullable: false),
                    PresencePenalty = table.Column<float>(type: "REAL", nullable: false),
                    StopSequences = table.Column<string>(type: "TEXT", nullable: true),
                    EvalAvgScore = table.Column<double>(type: "REAL", maxLength: 3, nullable: false),
                    EvalMaxScore = table.Column<double>(type: "REAL", maxLength: 3, nullable: false),
                    ExpectedResultsJson = table.Column<string>(type: "TEXT", nullable: true),
                    FullVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RangeName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Tactic = table.Column<string>(type: "TEXT", nullable: true),
                    Aiming = table.Column<int>(type: "INTEGER", maxLength: 5, nullable: false),
                    ParentTac = table.Column<string>(type: "TEXT", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Suffix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    VariableDictJson = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsShare = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDraft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptRange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RangeName = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptRange", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LlmModelId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultString = table.Column<string>(type: "TEXT", nullable: true),
                    CostTime = table.Column<double>(type: "REAL", nullable: false),
                    RobotScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    HumanScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    FinalScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    TestType = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptCostToken = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultCostToken = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalCostToken = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptItemVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
