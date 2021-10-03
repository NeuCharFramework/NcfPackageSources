using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    [Table(Register.DATABASE_PREFIX + nameof(DbConfig))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class DbConfig : EntityBase<int>
    {
        /// <summary>
        /// 备份间隔时间
        /// </summary>
        [Required]
        public int BackupCycleMinutes { get; private set; }
        /// <summary>
        /// 备份物理路径
        /// </summary>
        [MaxLength(300)]
        public string BackupPath { get; private set; }

        /// <summary>
        /// 上次备份时间
        /// </summary>
        public DateTime LastBackupTime { get; private set; }

        private DbConfig() { }

        public DbConfig(int backupCycleMinutes, string backupPath)
        {
            BackupCycleMinutes = backupCycleMinutes;
            BackupPath = backupPath;
            LastBackupTime = SystemTime.Now.AddDays(-1).DateTime;
        }

        public void RecordBackupTime()
        {
            LastBackupTime = SystemTime.Now.DateTime;
        }
    }
}
