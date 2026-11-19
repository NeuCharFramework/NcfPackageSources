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
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int BackupCycleMinutes { get; private set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        internal bool IsAutoBackup()
        {
            return this.BackupCycleMinutes > 0;
        }

        internal void ChangeBackupCycleMinutes(int bakupCycleMinutes) {

            this.BackupCycleMinutes = bakupCycleMinutes;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(300)]
        public string BackupPath { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime LastBackupTime { get; private set; }

        private DbConfig() { }

        public DbConfig(int backupCycleMinutes, string backupPath)
        {
            BackupCycleMinutes = backupCycleMinutes;
            BackupPath = backupPath;
            LastBackupTime = SystemTime.Now.AddDays(-1).DateTime;
        }

        public void SetConfig(int backupCycleMinutes, string backupPath)
        {
            BackupCycleMinutes = backupCycleMinutes;
            BackupPath = backupPath;
            RecordBackupTime();
        }

        public void RecordBackupTime()
        {
            LastBackupTime = SystemTime.Now.DateTime;
        }
    }
}
