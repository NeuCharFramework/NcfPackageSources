using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_PromptResultChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_LlModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: false),
                    ModelType = table.Column<int>(type: "INT", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    MaxToken = table.Column<int>(type: "INT", nullable: false),
                    IsShared = table.Column<bool>(type: "BIT", nullable: false),
                    Show = table.Column<bool>(type: "BIT", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_LlModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    RangeId = table.Column<int>(type: "INT", nullable: false),
                    NickName = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    ModelId = table.Column<int>(type: "INT", nullable: false),
                    TopP = table.Column<float>(type: "REAL", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", nullable: false),
                    MaxToken = table.Column<int>(type: "INT", nullable: false),
                    FrequencyPenalty = table.Column<float>(type: "REAL", nullable: false),
                    PresencePenalty = table.Column<float>(type: "REAL", nullable: false),
                    StopSequences = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    EvalAvgScore = table.Column<decimal>(type: "DECIMAL(29,4)", maxLength: 3, nullable: false),
                    EvalMaxScore = table.Column<decimal>(type: "DECIMAL(29,4)", maxLength: 3, nullable: false),
                    ExpectedResultsJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    IsAIGrade = table.Column<bool>(type: "BIT", nullable: false),
                    FullVersion = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    RangeName = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Tactic = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Aiming = table.Column<int>(type: "INT", maxLength: 5, nullable: false),
                    ParentTac = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    Prefix = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: true),
                    Suffix = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: true),
                    VariableDictJson = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    IsShare = table.Column<bool>(type: "BIT", nullable: false),
                    IsDraft = table.Column<bool>(type: "BIT", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptRange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    RangeName = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptRange", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    LlmModelId = table.Column<int>(type: "INT", nullable: false),
                    ResultString = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    CostTime = table.Column<double>(type: "FLOAT", nullable: false),
                    RobotScore = table.Column<decimal>(type: "DECIMAL(29,4)", nullable: false),
                    HumanScore = table.Column<decimal>(type: "DECIMAL(29,4)", nullable: false),
                    FinalScore = table.Column<decimal>(type: "DECIMAL(29,4)", nullable: false),
                    TestType = table.Column<int>(type: "INT", nullable: false),
                    PromptCostToken = table.Column<int>(type: "INT", nullable: false),
                    ResultCostToken = table.Column<int>(type: "INT", nullable: false),
                    TotalCostToken = table.Column<int>(type: "INT", nullable: false),
                    PromptItemId = table.Column<int>(type: "INT", nullable: false),
                    PromptItemVersion = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Mode = table.Column<int>(type: "INT", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptResult", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_PromptRange_PromptResultChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    PromptResultId = table.Column<int>(type: "INT", nullable: false),
                    RoleType = table.Column<int>(type: "INT", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    Sequence = table.Column<int>(type: "INT", nullable: false),
                    UserFeedback = table.Column<bool>(type: "BIT", nullable: true),
                    UserScore = table.Column<decimal>(type: "DECIMAL(29,4)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_PromptRange_PromptResultChat", x => x.Id);
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

            migrationBuilder.DropTable(
                name: "Senparc_PromptRange_PromptResultChat");
        }
    }
}
