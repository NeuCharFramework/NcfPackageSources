/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261110164028_AddConnectorInstance.cs
    文件功能描述：数据库迁移定义


    创建标识：Senparc - 20220818

    修改标识：Senparc - 20261112
    修改描述：v0.1.0 同步数据库迁移定义

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.Connectors.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddConnectorInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_Connectors_ConnectorInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PublicId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    ConnectorKey = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    Enabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ConfigJson = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    EnabledActionsJson = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    LastTestSucceeded = table.Column<bool>(type: "NUMBER(1)", nullable: true),
                    LastTestMessage = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    LastTestedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_Connectors_ConnectorInstance", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_Connectors_ConnectorInstance_PublicId",
                table: "Senparc_Connectors_ConnectorInstance",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_Connectors_ConnectorInstance");
        }
    }
}
