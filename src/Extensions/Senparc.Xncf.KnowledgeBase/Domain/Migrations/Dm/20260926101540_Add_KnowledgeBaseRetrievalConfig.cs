/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260926101540_Add_KnowledgeBaseRetrievalConfig.cs
    File description: Entity Framework database migration implementation.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261103
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_KnowledgeBaseRetrievalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetrievalConfigJson",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "NVARCHAR2(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetrievalConfigJson",
                table: "Senparc_KnowledgeBase_KnowledgeBase");
        }
    }
}
