using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_KnowledgeBase_Manage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_KnowledgeBase_KnowledgeBases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EmbeddingModelId = table.Column<string>(type: "TEXT", nullable: true),
                    VectorDBId = table.Column<string>(type: "TEXT", nullable: true),
                    ChatModelId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_KnowledgeBase_KnowledgeBases", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_KnowledgeBase_KnowledgeBases");
        }
    }
}
