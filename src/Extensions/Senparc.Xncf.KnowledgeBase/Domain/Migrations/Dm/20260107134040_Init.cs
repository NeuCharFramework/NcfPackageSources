using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_KnowledgeBase_Color",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Red = table.Column<int>(type: "INT", nullable: false),
                    Green = table.Column<int>(type: "INT", nullable: false),
                    Blue = table.Column<int>(type: "INT", nullable: false),
                    AdditionNote = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_KnowledgeBase_Color", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_KnowledgeBase_KnowledgeBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    EmbeddingModelId = table.Column<int>(type: "INT", nullable: false),
                    VectorDBId = table.Column<int>(type: "INT", nullable: false),
                    ChatModelId = table.Column<int>(type: "INT", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Content = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_KnowledgeBase_KnowledgeBase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_KnowledgeBase_KnowledgeBaseItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    KnowledgeBasesId = table.Column<int>(type: "INT", nullable: false),
                    ContentType = table.Column<int>(type: "INT", nullable: false),
                    Content = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    FileName = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ChunkIndex = table.Column<int>(type: "INT", nullable: false),
                    IsEmbedded = table.Column<bool>(type: "BIT", nullable: false),
                    EmbeddedTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_KnowledgeBase_KnowledgeBaseItem", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_KnowledgeBase_Color");

            migrationBuilder.DropTable(
                name: "Senparc_KnowledgeBase_KnowledgeBase");

            migrationBuilder.DropTable(
                name: "Senparc_KnowledgeBase_KnowledgeBaseItem");
        }
    }
}
