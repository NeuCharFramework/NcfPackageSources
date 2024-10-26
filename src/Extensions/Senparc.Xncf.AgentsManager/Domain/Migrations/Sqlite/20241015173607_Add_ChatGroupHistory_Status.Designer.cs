﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.AgentsManager.Models;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Sqlite
{
    [DbContext(typeof(AgentsManagerSenparcEntities_Sqlite))]
    [Migration("20241015173607_Add_ChatGroupHistory_Status")]
    partial class Add_ChatGroupHistory_Status
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enable")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HookRobotParameter")
                        .HasColumnType("TEXT");

                    b.Property<int>("HookRobotType")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PromptCode")
                        .HasColumnType("TEXT");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("SystemMessage")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TenantId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Senparc_AgentsManager_AgentTemplate");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("AdminAgentTemplateId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enable")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EnterAgentTemplateId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TenantId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AdminAgentTemplateId");

                    b.HasIndex("EnterAgentTemplateId");

                    b.ToTable("Senparc_AgentsManager_ChatGroup");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroupHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("ChatGroupId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("FromAgentTemplateId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("MessageType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TenantId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ToAgentTemplateId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ChatGroupId");

                    b.HasIndex("FromAgentTemplateId");

                    b.HasIndex("ToAgentTemplateId");

                    b.ToTable("Senparc_AgentsManager_ChatGroupHistory");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroupMember", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("AgentTemplateId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ChatGroupId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<int>("TenantId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UID")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AgentTemplateId");

                    b.ToTable("Senparc_AgentsManager_ChatGroupMember");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroup", b =>
                {
                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", "AdminAgentTemplate")
                        .WithMany("AdminChatGroups")
                        .HasForeignKey("AdminAgentTemplateId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", "EnterAgentTemplate")
                        .WithMany("EnterAgentChatGroups")
                        .HasForeignKey("EnterAgentTemplateId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("AdminAgentTemplate");

                    b.Navigation("EnterAgentTemplate");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroupHistory", b =>
                {
                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroup", "ChatGroup")
                        .WithMany()
                        .HasForeignKey("ChatGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", "FromAgentTemplate")
                        .WithMany("FromChatGroupHistories")
                        .HasForeignKey("FromAgentTemplateId");

                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", "ToAgentTemplate")
                        .WithMany("ToChatGroupHistoies")
                        .HasForeignKey("ToAgentTemplateId");

                    b.Navigation("ChatGroup");

                    b.Navigation("FromAgentTemplate");

                    b.Navigation("ToAgentTemplate");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.ChatGroupMember", b =>
                {
                    b.HasOne("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", "AgentTemplate")
                        .WithMany("ChatGroupMembers")
                        .HasForeignKey("AgentTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AgentTemplate");
                });

            modelBuilder.Entity("Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate", b =>
                {
                    b.Navigation("AdminChatGroups");

                    b.Navigation("ChatGroupMembers");

                    b.Navigation("EnterAgentChatGroups");

                    b.Navigation("FromChatGroupHistories");

                    b.Navigation("ToChatGroupHistoies");
                });
#pragma warning restore 612, 618
        }
    }
}
