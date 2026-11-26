/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DbConfig.cs
    File description: DbConfig related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    [Table(Register.DATABASE_PREFIX + nameof(DbConfig))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class DbConfig : EntityBase<int>
    {
        /// <summary>
        /// Gets the configured automatic backup interval in minutes.
        /// </summary>
        [Required]
        public int BackupCycleMinutes { get; private set; }


        /// <summary>
        /// Determines whether automatic backup is enabled.
        /// </summary>
        /// <returns><see langword="true"/> when the backup interval is positive.</returns>
        internal bool IsAutoBackup()
        {
            return this.BackupCycleMinutes > 0;
        }

        /// <summary>
        /// Changes the automatic backup interval without recording a backup event.
        /// </summary>
        /// <param name="bakupCycleMinutes">The new interval in minutes.</param>
        internal void ChangeBackupCycleMinutes(int bakupCycleMinutes) {

            this.BackupCycleMinutes = bakupCycleMinutes;
        }

        /// <summary>
        /// Gets the directory used for database backup files.
        /// </summary>
        [MaxLength(300)]
        public string BackupPath { get; private set; }

        /// <summary>
        /// Gets the time of the most recent recorded backup.
        /// </summary>
        public DateTime LastBackupTime { get; private set; }

        private DbConfig() { }

        /// <summary>
        /// Initializes database backup settings.
        /// </summary>
        /// <param name="backupCycleMinutes">The automatic backup interval in minutes.</param>
        /// <param name="backupPath">The backup directory.</param>
        public DbConfig(int backupCycleMinutes, string backupPath)
        {
            BackupCycleMinutes = backupCycleMinutes;
            BackupPath = backupPath;
            LastBackupTime = SystemTime.Now.AddDays(-1).DateTime;
        }

        /// <summary>
        /// Updates backup settings and records the current time.
        /// </summary>
        /// <param name="backupCycleMinutes">The automatic backup interval in minutes.</param>
        /// <param name="backupPath">The backup directory.</param>
        public void SetConfig(int backupCycleMinutes, string backupPath)
        {
            BackupCycleMinutes = backupCycleMinutes;
            BackupPath = backupPath;
            RecordBackupTime();
        }

        /// <summary>
        /// Records the current system time as the most recent backup time.
        /// </summary>
        public void RecordBackupTime()
        {
            LastBackupTime = SystemTime.Now.DateTime;
        }
    }
}
