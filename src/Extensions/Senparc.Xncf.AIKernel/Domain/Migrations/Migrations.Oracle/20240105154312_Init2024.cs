using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Init2024 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: true),
                    AiPlatform = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    OrganizationId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
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
                    table.PrimaryKey("PK_Senparc_AIKernel_AIModel", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIModel");
        }
    }
}
