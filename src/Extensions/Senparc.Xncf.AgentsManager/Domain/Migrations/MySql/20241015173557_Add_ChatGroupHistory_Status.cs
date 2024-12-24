using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_ChatGroupHistory_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_AgentTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SystemMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PromptCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HookRobotType = table.Column<int>(type: "int", nullable: false),
                    HookRobotParameter = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_AgentTemplate", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Enable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminAgentTemplateId = table.Column<int>(type: "int", nullable: false),
                    EnterAgentTemplateId = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroup_Senparc_AgentsManager_AgentT~",
                        column: x => x.AdminAgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroup_Senparc_AgentsManager_Agent~1",
                        column: x => x.EnterAgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroupMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentTemplateId = table.Column<int>(type: "int", nullable: false),
                    ChatGroupId = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroupMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupMember_Senparc_AgentsManager_~",
                        column: x => x.AgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Senparc_AgentsManager_ChatGroupHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChatGroupId = table.Column<int>(type: "int", nullable: false),
                    FromAgentTemplateId = table.Column<int>(type: "int", nullable: true),
                    ToAgentTemplateId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AgentsManager_ChatGroupHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManager~",
                        column: x => x.ChatGroupId,
                        principalTable: "Senparc_AgentsManager_ChatGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~1",
                        column: x => x.FromAgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Senparc_AgentsManager_ChatGroupHistory_Senparc_AgentsManage~2",
                        column: x => x.ToAgentTemplateId,
                        principalTable: "Senparc_AgentsManager_AgentTemplate",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroup_AdminAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup",
                column: "AdminAgentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroup_EnterAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroup",
                column: "EnterAgentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_ChatGroupId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_FromAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "FromAgentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupHistory_ToAgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroupHistory",
                column: "ToAgentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Senparc_AgentsManager_ChatGroupMember_AgentTemplateId",
                table: "Senparc_AgentsManager_ChatGroupMember",
                column: "AgentTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_ChatGroupHistory");

            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_ChatGroupMember");

            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_ChatGroup");

            migrationBuilder.DropTable(
                name: "Senparc_AgentsManager_AgentTemplate");
        }
    }
}
