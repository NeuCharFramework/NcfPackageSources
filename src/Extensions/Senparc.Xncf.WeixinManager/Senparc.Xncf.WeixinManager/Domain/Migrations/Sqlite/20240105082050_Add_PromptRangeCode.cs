using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.WeixinManager.Domain.Migrations.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Add_PromptRangeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeixinManager_MpAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Logo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AppId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AppSecret = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EncodingAESKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PromptRangeCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeixinManager_MpAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeixinManager_UserTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MpAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MpAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Subscribe = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenId = table.Column<string>(type: "TEXT", nullable: false),
                    NickName = table.Column<string>(type: "TEXT", nullable: false),
                    Sex = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Province = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    HeadImgUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Subscribe_Time = table.Column<long>(type: "INTEGER", nullable: false),
                    UnionId = table.Column<string>(type: "TEXT", nullable: true),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    Groupid = table.Column<int>(type: "INTEGER", nullable: false),
                    Subscribe_Scene = table.Column<string>(type: "TEXT", nullable: true),
                    Qr_Scene = table.Column<uint>(type: "INTEGER", nullable: false),
                    Qr_Scene_Str = table.Column<string>(type: "TEXT", nullable: true),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AdminRemark = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
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
                    UserTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeixinUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Flag = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
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
