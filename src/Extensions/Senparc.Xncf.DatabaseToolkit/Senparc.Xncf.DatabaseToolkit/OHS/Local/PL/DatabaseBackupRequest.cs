/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: DatabaseBackupRequest.cs

    File description: DatabaseBackupRequest related implementation

    

    

    Created by: Senparc - 20211014

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260717

    Change description: v0.26.0-preview2 Updated module behavior and related support.



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
