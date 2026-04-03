using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    [Table(Register.DATABASE_PREFIX + nameof(DbConfig))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class DbConfig : EntityBase<int>
    {
        /// <summary>
        /// backup interval
        /// </summary>
        [Required]
        public int BackupCycleMinutes { get; private set; }


        /// <summary>
        /// Whether backup is enabled
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
        ///Backup physical path
        /// </summary>
        [MaxLength(300)]
        public string BackupPath { get; private set; }

        /// <summary>
        ///Last backup time
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
