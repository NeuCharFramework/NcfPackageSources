/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260803132803_Add_KnowledgeBaseLifecycle.cs
    File description: Entity Framework database migration implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_KnowledgeBaseLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NcfFileId",
                table: "Senparc_KnowledgeBase_KnowledgeBaseItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VectorCollectionName",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_KnowledgeBase_KnowledgeBaseItem_KnowledgeBasesId_NcfFileId_ChunkIndex",
                table: "Senparc_KnowledgeBase_KnowledgeBaseItem",
                columns: new[] { "KnowledgeBasesId", "NcfFileId", "ChunkIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Senparc_KnowledgeBase_KnowledgeBaseItem_KnowledgeBasesId_NcfFileId_ChunkIndex",
                table: "Senparc_KnowledgeBase_KnowledgeBaseItem");

            migrationBuilder.DropColumn(
                name: "NcfFileId",
                table: "Senparc_KnowledgeBase_KnowledgeBaseItem");

            migrationBuilder.DropColumn(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBase");

            migrationBuilder.DropColumn(
                name: "VectorCollectionName",
                table: "Senparc_KnowledgeBase_KnowledgeBase");
        }
    }
}
