using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class AddWeixinClaw : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    BotTokenProtected = table.Column<string>(type: "NVARCHAR2(4096)", maxLength: 4096, nullable: true),
                    IlinkBotId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    IlinkUserId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    GetUpdatesBuf = table.Column<string>(type: "clob", maxLength: 100000, nullable: true),
                    PromptRangeCode = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Enabled = table.Column<bool>(type: "BIT", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    LastConnectedAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawMessageReceipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    WeixinClawAccountId = table.Column<int>(type: "INT", nullable: false),
                    MessageId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Seq = table.Column<long>(type: "BIGINT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawMessageReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeixinManager_WeixinClawMessageReceipt_WeixinManager_WeixinClawAccount_WeixinClawAccountId",
                        column: x => x.WeixinClawAccountId,
                        principalTable: "WeixinManager_WeixinClawAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawAccount_IlinkBotId",
                table: "WeixinManager_WeixinClawAccount",
                column: "IlinkBotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageReceipt_WeixinClawAccountId_MessageId_Seq",
                table: "WeixinManager_WeixinClawMessageReceipt",
                columns: new[] { "WeixinClawAccountId", "MessageId", "Seq" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawMessageReceipt");

            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawAccount");
        }
    }
}
