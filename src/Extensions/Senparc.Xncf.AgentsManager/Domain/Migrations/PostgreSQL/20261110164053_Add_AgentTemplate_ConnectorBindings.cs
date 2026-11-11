/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20261110164053_Add_AgentTemplate_ConnectorBindings.cs
    文件功能描述：数据库迁移定义


    创建标识：Senparc - 20250120

    修改标识：Senparc - 20261112
    修改描述：v0.19.3 同步数据库迁移定义

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class Add_AgentTemplate_ConnectorBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConnectorBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectorBindingsJson",
                table: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
