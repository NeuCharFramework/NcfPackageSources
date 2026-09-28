using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class AddWeixinClawBindingProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinClawBindingProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    WeixinClawAccountId = table.Column<int>(type: "INT", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    AdminUserId = table.Column<int>(type: "INT", nullable: false),
                    WorkflowId = table.Column<int>(type: "INT", nullable: true),
                    AiModelId = table.Column<int>(type: "INT", nullable: false),
                    Mode = table.Column<int>(type: "INT", nullable: false),
                    EnableNeuBell = table.Column<bool>(type: "BIT", nullable: false),
                    EnableWorkflow = table.Column<bool>(type: "BIT", nullable: false),
                    Enabled = table.Column<bool>(type: "BIT", nullable: false),
                    BindingCodeHash = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinClawBindingProfile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawBindingProfile_WeixinClawAccountId_BindingCodeHash",
                table: "WeixinManager_WeixinClawBindingProfile",
                columns: new[] { "WeixinClawAccountId", "BindingCodeHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinClawBindingProfile_WeixinClawAccountId_Enabled",
                table: "WeixinManager_WeixinClawBindingProfile",
                columns: new[] { "WeixinClawAccountId", "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinClawBindingProfile");
        }
    }
}
