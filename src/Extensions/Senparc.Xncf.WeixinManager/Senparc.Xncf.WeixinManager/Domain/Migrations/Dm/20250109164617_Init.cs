using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_MpAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    Logo = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    AppId = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    AppSecret = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    EncodingAESKey = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    PromptRangeCode = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_MpAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeixinManager_UserTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    MpAccountId = table.Column<int>(type: "INT", nullable: false),
                    TagId = table.Column<int>(type: "INT", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    Count = table.Column<int>(type: "INT", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_UserTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK__UserTag__MpAccountId",
                        column: x => x.MpAccountId,
                        principalTable: "WeixinManager_MpAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeixinManager_WeixinUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INT", nullable: false)
                        .Annotation("Dm:Identity", "1, 1"),
                    MpAccountId = table.Column<int>(type: "INT", nullable: false),
                    Subscribe = table.Column<int>(type: "INT", nullable: false),
                    OpenId = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    NickName = table.Column<string>(type: "NVARCHAR2(32767)", nullable: false),
                    Sex = table.Column<int>(type: "INT", nullable: false),
                    Language = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    City = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Province = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Country = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    HeadImgUrl = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Subscribe_Time = table.Column<long>(type: "BIGINT", nullable: false),
                    UnionId = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Groupid = table.Column<int>(type: "INT", nullable: false),
                    Subscribe_Scene = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Qr_Scene = table.Column<long>(type: "BIGINT", nullable: false),
                    Qr_Scene_Str = table.Column<string>(type: "NVARCHAR2(32767)", nullable: true),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_WeixinUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK__WeixinUser__MpAccountId",
                        column: x => x.MpAccountId,
                        principalTable: "WeixinManager_MpAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeixinManager_UserTag_WeixinUser",
                columns: table => new
                {
                    UserTagId = table.Column<int>(type: "INT", nullable: false),
                    WeixinUserId = table.Column<int>(type: "INT", nullable: false),
                    Flag = table.Column<bool>(type: "BIT", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    TenantId = table.Column<int>(type: "INT", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_UserTag_WeixinUser", x => new { x.UserTagId, x.WeixinUserId });
                    table.ForeignKey(
                        name: "FK_WeixinManager_UserTag_WeixinUser_WeixinManager_UserTag_UserTagId",
                        column: x => x.UserTagId,
                        principalTable: "WeixinManager_UserTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeixinManager_UserTag_WeixinUser_WeixinManager_WeixinUser_WeixinUserId",
                        column: x => x.WeixinUserId,
                        principalTable: "WeixinManager_WeixinUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_UserTag_MpAccountId",
                table: "WeixinManager_UserTag",
                column: "MpAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_UserTag_WeixinUser_WeixinUserId",
                table: "WeixinManager_UserTag_WeixinUser",
                column: "WeixinUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeixinManager_WeixinUser_MpAccountId",
                table: "WeixinManager_WeixinUser",
                column: "MpAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeixinManager_UserTag_WeixinUser");

            migrationBuilder.DropTable(
                name: "WeixinManager_UserTag");

            migrationBuilder.DropTable(
                name: "WeixinManager_WeixinUser");

            migrationBuilder.DropTable(
                name: "WeixinManager_MpAccount");
        }
    }
}
