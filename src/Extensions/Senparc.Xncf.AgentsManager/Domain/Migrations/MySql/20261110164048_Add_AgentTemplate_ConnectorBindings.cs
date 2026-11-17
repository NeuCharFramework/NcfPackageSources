/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20261110164048_Add_AgentTemplate_ConnectorBindings.cs
    File description: database migration definition


    Created by: Senparc - 20250120

    Modified by: Senparc - 20261112
    Change description: v0.19.3 synchronize the database migration definition

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
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
