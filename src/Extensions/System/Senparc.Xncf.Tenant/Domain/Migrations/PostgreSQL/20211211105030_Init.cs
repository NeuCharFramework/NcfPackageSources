using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Senparc.Xncf.Tenant.Domain.Migrations.PostgreSQL
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false),
                    TenantKey = table.Column<string>(type: "text", nullable: false),
                    Flag = table.Column<bool>(type: "boolean", nullable: false),
                    AddTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AdminRemark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfos", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInfos");
        }
    }
}
