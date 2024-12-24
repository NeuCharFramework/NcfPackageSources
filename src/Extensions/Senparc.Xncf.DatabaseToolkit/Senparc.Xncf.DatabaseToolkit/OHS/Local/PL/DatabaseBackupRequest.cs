using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.PL
{
    public enum BackupDatabaseOptions
    {
        如果文件存在则不覆盖 = 0,
        校验备份成功 = 1
    }

    public class DatabaseBackup_BackupRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [Description("路径||本地物理路径，如：E:\\Senparc\\Database-Backup\\NCF.bak，必须包含文件名。请确保此路径有网站程序访问权限！")]
        public string Path { get; set; }

        [Description("选项||备份数据库选项")]
        public SelectionList Options { get; set; } = new SelectionList(SelectionType.CheckBoxList,
            new[] {
                    new SelectionItem($"{(int)BackupDatabaseOptions.如果文件存在则不覆盖}","如果文件存在，则不覆盖","文件已存在的情况下，不会执行备份操作"),
                    new SelectionItem($"{(int)BackupDatabaseOptions.校验备份成功}","备份完成后校验.bak文件是否更新成功","此操作只能检查文件是否被更新，无法检测文件内部内容",true),
            });

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            var configService = serviceProvider.GetService<ServiceBase<DbConfig>>();
            var config = await configService.GetObjectAsync(z => true);
            if (config != null)
            {
                Path = config.BackupPath;
            }
        }
    }
}
