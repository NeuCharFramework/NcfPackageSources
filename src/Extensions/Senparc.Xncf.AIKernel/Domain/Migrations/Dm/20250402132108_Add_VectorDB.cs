using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_VectorDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    ModelId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DeploymentName = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    Endpoint = table.Column<string>(type: "NVARCHAR2(250)", maxLength: 250, nullable: true),
                    AiPlatform = table.Column<int>(type: "INT", nullable: false),
                    ConfigModelType = table.Column<int>(type: "INT", nullable: false),
                    OrganizationId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiKey = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    ApiVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
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
                    table.PrimaryKey("PK_Senparc_AIKernel_AIModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIVector",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    VectorId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    ConnectionString = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    VectorDBType = table.Column<int>(type: "INT", nullable: false),
                    Note = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
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
                    table.PrimaryKey("PK_Senparc_AIKernel_AIVector", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIModel");

            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIVector");
        }
    }
}
