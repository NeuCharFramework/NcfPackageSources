using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Dm
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
                type: "INT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VectorCollectionName",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "NVARCHAR2(200)",
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
