using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Sqlite
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
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddedTime",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmbedded",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "VectorDBId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmbeddingModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChatModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);
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
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBasesDetail",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<string>(
                name: "VectorDBId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "EmbeddingModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ChatModelId",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Senparc_KnowledgeBase_KnowledgeBases",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
