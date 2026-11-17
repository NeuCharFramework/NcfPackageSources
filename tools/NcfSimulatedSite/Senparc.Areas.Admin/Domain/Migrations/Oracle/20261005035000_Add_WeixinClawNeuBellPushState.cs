/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: 20261005035000_Add_WeixinClawNeuBellPushState.cs
    File description: 20261005035000_Add_WeixinClawNeuBellPushState related definitions and implementation


    Created by: Senparc - 20261006

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Areas.Admin.Domain.Models;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle;

[DbContext(typeof(AdminSenparcEntities_Oracle))]
[Migration("20261005035000_Add_WeixinClawNeuBellPushState")]
public partial class Add_WeixinClawNeuBellPushState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding",
            type: "NVARCHAR2(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NeuBellPushStateJson",
            table: "ADMIN_WeixinClawAdminBinding");
    }
}
