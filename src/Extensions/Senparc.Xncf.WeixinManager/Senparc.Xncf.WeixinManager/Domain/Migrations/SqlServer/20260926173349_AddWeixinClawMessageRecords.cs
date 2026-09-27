using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddWeixinClawMessageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawMessageRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeixinClawAccountId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Seq = table.Column<long>(type: "bigint", nullable: true),
                    FromUserId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ToUserId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    MessageState = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Flag = table.Column<bool>(type: "bit", nullable: false),
                    AddTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AdminRemark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawMessageRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_CreatedAt",
                table: "WeixinManager_WeixinClawMessageRecord",
                columns: new[] { "WeixinClawAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawMessageRecord_WeixinClawAccountId_Direction_MessageId",
                table: "WeixinManager_WeixinClawMessageRecord",
                columns: new[] { "WeixinClawAccountId", "Direction", "MessageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawMessageRecord");
        }
    }
}
