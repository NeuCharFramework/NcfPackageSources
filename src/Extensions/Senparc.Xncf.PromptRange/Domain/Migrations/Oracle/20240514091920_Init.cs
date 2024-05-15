using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: false),
                    ModelType = table.Column<int>(type: "NUMBER(10)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    MaxToken = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IsShared = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Show = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    RangeId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NickName = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ModelId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TopP = table.Column<float>(type: "BINARY_FLOAT", nullable: false),
                    Temperature = table.Column<float>(type: "BINARY_FLOAT", nullable: false),
                    MaxToken = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FrequencyPenalty = table.Column<float>(type: "BINARY_FLOAT", nullable: false),
                    PresencePenalty = table.Column<float>(type: "BINARY_FLOAT", nullable: false),
                    StopSequences = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    EvalAvgScore = table.Column<decimal>(type: "DECIMAL(18, 2)", maxLength: 3, nullable: false),
                    EvalMaxScore = table.Column<decimal>(type: "DECIMAL(18, 2)", maxLength: 3, nullable: false),
                    ExpectedResultsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    FullVersion = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    RangeName = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Tactic = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Aiming = table.Column<int>(type: "NUMBER(10)", maxLength: 5, nullable: false),
                    ParentTac = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Prefix = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: true),
                    Suffix = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: true),
                    VariableDictJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    IsShare = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    IsDraft = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    RangeName = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    LlmModelId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ResultString = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CostTime = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    RobotScore = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    HumanScore = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    FinalScore = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    TestType = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PromptCostToken = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ResultCostToken = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TotalCostToken = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PromptItemId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PromptItemVersion = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
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
