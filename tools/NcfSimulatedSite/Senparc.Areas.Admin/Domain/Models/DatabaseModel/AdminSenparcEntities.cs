/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AdminSenparcEntities.cs
    File description: Related implementation.


    Created by: Senparc - 20241028

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260809
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261102
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 implement AdminChat memory storage and management system

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using System;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// The current Entities exist only to help SenparcEntities generate migration information and have no special operational purpose.
    /// </summary>
    public class AdminSenparcEntities : XncfDatabaseDbContext
    {
        public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminUserInfo> SystemConfigs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminAuthConfig> AdminAuthConfigs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminChatSession> AdminChatSessions { get; set; }
        public DbSet<AdminChatMemoryState> AdminChatMemoryStates { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<AdminChatSessionWorkflow> AdminChatSessionWorkflows { get; set; }

        /// <summary>
        /// Admin Chat Harness trajectories.
        /// </summary>
        public DbSet<AdminChatTrajectory> AdminChatTrajectories { get; set; }

        /// <summary>
        /// Append-only Admin Chat Harness trajectory events.
        /// </summary>
        public DbSet<AdminChatTrajectoryEvent> AdminChatTrajectoryEvents { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharPivotConfiguration> NeuCharPivotConfigurations { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharPivotFunction> NeuCharPivotFunctions { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharPivotLoopTask> NeuCharPivotLoopTasks { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharExecutionLog> NeuCharExecutionLogs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharPivotBoard> NeuCharPivotBoards { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuBellWebHook> NeuBellWebHooks { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuBellWebHookLog> NeuBellWebHookLogs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<WeixinClawAdminBinding> WeixinClawAdminBindings { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<NeuCharFunctionProvitAccess> NeuCharFunctionProvitAccesses { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NeuCharPivotConfiguration>()
                .HasIndex(z => z.ModuleUid)
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotFunction>()
                .HasIndex(z => new { z.PivotId, z.FunctionKey })
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotFunction>()
                .HasIndex(z => z.ModuleUid);
            modelBuilder.Entity<NeuCharPivotLoopTask>()
                .HasIndex(z => z.FunctionId)
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotLoopTask>()
                .HasIndex(z => new { z.Enabled, z.NextRunAt });
            modelBuilder.Entity<NeuCharPivotLoopTask>()
                .HasIndex(z => z.WorkflowId);
            modelBuilder.Entity<NeuCharExecutionLog>()
                .HasIndex(z => z.CorrelationId);
            modelBuilder.Entity<NeuCharExecutionLog>()
                .HasIndex(z => new { z.SourceType, z.SourceId });
            modelBuilder.Entity<AdminChatSessionWorkflow>()
                .HasIndex(z => new { z.SessionId, z.WorkflowId })
                .IsUnique();
            modelBuilder.Entity<AdminChatTrajectory>()
                .HasIndex(z => new { z.SessionId, z.UserId, z.AddTime });
            modelBuilder.Entity<AdminChatTrajectoryEvent>()
                .HasIndex(z => new { z.TrajectoryId, z.Sequence })
                .IsUnique();
            modelBuilder.Entity<NeuCharPivotBoard>()
                .HasIndex(z => z.PageKey);
            modelBuilder.Entity<NeuBellWebHook>()
                .HasIndex(z => z.ProviderFilter);
            modelBuilder.Entity<NeuBellWebHook>()
                .Property(z => z.HttpMethod)
                .HasDefaultValue(NeuBellWebHook.MethodPost);
            modelBuilder.Entity<NeuBellWebHookLog>()
                .HasIndex(z => z.AddTime);
            modelBuilder.Entity<WeixinClawAdminBinding>()
                .HasIndex(z => new { z.AccountId, z.FromUserId, z.GroupId })
                .IsUnique();
            modelBuilder.Entity<WeixinClawAdminBinding>()
                .HasIndex(z => new { z.Enabled, z.EnableNeuBell });
            modelBuilder.Entity<NeuCharFunctionProvitAccess>()
                .HasIndex(z => new { z.ModuleUid, z.FunctionKey })
                .IsUnique();

            var providerName = Database.ProviderName ?? string.Empty;
            if (providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase)
                || providerName.Contains("Dm", StringComparison.OrdinalIgnoreCase))
                modelBuilder.Entity<AdminChatMemoryState>().Property(row => row.Payload).HasColumnType("BLOB");
            var largeTextType = providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase)
                ? "NCLOB"
                : providerName.Contains("Dm", StringComparison.OrdinalIgnoreCase)
                    ? "NVARCHAR2(32767)"
                    : providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                        ? "longtext"
                        : providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                            ? "nvarchar(max)"
                            : "TEXT";

            SetLargeText<NeuCharPivotConfiguration>(modelBuilder, largeTextType,
                nameof(NeuCharPivotConfiguration.UserRequirement),
                nameof(NeuCharPivotConfiguration.LayoutSchemaJson),
                nameof(NeuCharPivotConfiguration.LastError));
            SetLargeText<NeuCharPivotFunction>(modelBuilder, largeTextType,
                nameof(NeuCharPivotFunction.Description),
                nameof(NeuCharPivotFunction.UiSchemaJson),
                nameof(NeuCharPivotFunction.DefaultParametersJson));
            SetLargeText<NeuCharPivotLoopTask>(modelBuilder, largeTextType,
                nameof(NeuCharPivotLoopTask.ParametersJson),
                nameof(NeuCharPivotLoopTask.LastError));
            SetLargeText<NeuCharExecutionLog>(modelBuilder, largeTextType,
                nameof(NeuCharExecutionLog.ResultSummary),
                nameof(NeuCharExecutionLog.Error));
            SetLargeText<NeuCharPivotBoard>(modelBuilder, largeTextType,
                nameof(NeuCharPivotBoard.Description),
                nameof(NeuCharPivotBoard.BlocksJson));
            SetLargeText<AdminChatTrajectory>(modelBuilder, largeTextType,
                nameof(AdminChatTrajectory.SessionStateJson),
                nameof(AdminChatTrajectory.LastError));
            SetLargeText<AdminChatTrajectoryEvent>(modelBuilder, largeTextType,
                nameof(AdminChatTrajectoryEvent.Content),
                nameof(AdminChatTrajectoryEvent.PayloadJson));
            SetLargeText<NeuBellWebHook>(modelBuilder, largeTextType,
                nameof(NeuBellWebHook.BodyTemplate));
            SetLargeText<NeuBellWebHookLog>(modelBuilder, largeTextType,
                nameof(NeuBellWebHookLog.Payload));
        }

        private static void SetLargeText<TEntity>(
            ModelBuilder modelBuilder,
            string columnType,
            params string[] propertyNames)
            where TEntity : class
        {
            var entity = modelBuilder.Entity<TEntity>();
            foreach (var propertyName in propertyNames)
            {
                entity.Property<string>(propertyName).HasColumnType(columnType);
            }
        }
    }
}
