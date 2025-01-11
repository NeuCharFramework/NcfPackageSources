using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template_OrgName.Xncf.Template_XncfName.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Template_OrgName_Template_XncfName_Color",
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
                    table.PrimaryKey("PK_Template_OrgName_Template_XncfName_Color", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Template_OrgName_Template_XncfName_Color");
        }
    }
}
