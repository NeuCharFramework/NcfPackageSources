using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Oracle
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    BotTokenProtected = table.Column<string>(type: "NCLOB", maxLength: 4096, nullable: true),
                    IlinkBotId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    IlinkUserId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    GetUpdatesBuf = table.Column<string>(type: "NCLOB", maxLength: 100000, nullable: true),
                    PromptRangeCode = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Enabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    LastError = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    LastConnectedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
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
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WeixinClawAccountId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MessageId = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Seq = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawMessageReceipt", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawAccount_IlinkBotId",
                table: "WeixinManager_WeixinClawAccount",
                column: "IlinkBotId");

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
