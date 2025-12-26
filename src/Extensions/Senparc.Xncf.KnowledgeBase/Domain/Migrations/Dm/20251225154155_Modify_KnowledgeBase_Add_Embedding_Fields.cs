using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Modify_KnowledgeBase_Add_Embedding_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "KnowledgeBasesId",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INT",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(32767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)")
                .Annotation("Dm:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INT",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "NVARCHAR2(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmbedded",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "BIT",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "VectorDBId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INT",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(32767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmbeddingModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INT",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(32767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChatModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INT",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(32767)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)")
                .Annotation("Dm:Identity", "1, 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail");

            migrationBuilder.DropColumn(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail");

            migrationBuilder.DropColumn(
                name: "IsEmbedded",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail");

            migrationBuilder.AlterColumn<string>(
                name: "KnowledgeBasesId",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "NVARCHAR2(32767)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INT")
                .OldAnnotation("Dm:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "VectorDBId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "NVARCHAR2(32767)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INT");

            migrationBuilder.AlterColumn<string>(
                name: "EmbeddingModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "NVARCHAR2(32767)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INT");

            migrationBuilder.AlterColumn<string>(
                name: "ChatModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "NVARCHAR2(32767)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INT")
                .OldAnnotation("Dm:Identity", "1, 1");
        }
    }
}
