/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseBackupRequest.cs
    文件功能描述：DatabaseBackupRequest 相关实现
    
    
    创建标识：Senparc - 20211014
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

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
        [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.DatabaseBackup.Path")]
        public string Path { get; set; }

        [LocalizedDescription(typeof(DatabaseToolkitResource), "Parameter.DatabaseBackup.Options")]
        [FunctionParameterUi(ParameterType.CheckBoxList, nameof(OptionsList))]
        public string[] Options { get; set; }

        [JsonIgnore]
        public SelectionList OptionsList { get; set; } = new SelectionList(SelectionType.CheckBoxList,
            new[] {
                    new SelectionItem($"{(int)BackupDatabaseOptions.如果文件存在则不覆盖}", DatabaseToolkitResource.Get("Parameter.DatabaseBackup.NoOverwrite"), DatabaseToolkitResource.Get("Parameter.DatabaseBackup.NoOverwrite.Help")),
                    new SelectionItem($"{(int)BackupDatabaseOptions.校验备份成功}", DatabaseToolkitResource.Get("Parameter.DatabaseBackup.Verify"), DatabaseToolkitResource.Get("Parameter.DatabaseBackup.Verify.Help"), true),
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
