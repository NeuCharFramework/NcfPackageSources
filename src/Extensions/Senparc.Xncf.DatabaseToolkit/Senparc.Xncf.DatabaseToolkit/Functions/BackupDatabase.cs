using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    public class BackupDatabase : FunctionBase
    {
        public enum BackupDatabaseOptions
        {
            如果文件存在则不覆盖 = 0,
            校验备份成功 = 1
        }

        public class BackupDatabase_Parameters : FunctionParameterLoadDataBase
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

        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "备份数据库";

        public override string Description => "将当前使用的数据库备份到指定路径。友情提示：建议确保该路径不具备公开访问权限！";

        public override Type FunctionParameterType => typeof(BackupDatabase_Parameters);

        public BackupDatabase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<BackupDatabase_Parameters>(param, (typeParam, sb, result) =>
            {
                try
                {
                    var path = typeParam.Path;

                    if (File.Exists(path))
                    {
                        var copyPath = path + ".last.bak";
                        if (typeParam.Options.SelectedValues.Contains($"{(int)BackupDatabaseOptions.如果文件存在则不覆盖}"))
                        {
                            RecordLog(sb, "检测到同名文件，停止覆盖。地址：" + copyPath);
                            result.Message = "检测到同名文件，停止覆盖！";
                            return;
                        }

                        RecordLog(sb, "检测到同名文件，已经移动到（并覆盖）：" + copyPath);
                        File.Move(path, copyPath, true);
                    }

                    RecordLog(sb, "开始获取 ISenparcEntities 对象");
                    var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntities)) as SenparcEntitiesBase;
                    RecordLog(sb, "获取 ISenparcEntities 对象成功");

                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    var backupSql = currentDatabaseConfiguration.GetBackupDatabaseSql(senparcEntities.Database.GetDbConnection(), path);
                    if (backupSql.IsNullOrEmpty())
                    {
                        RecordLog(sb, $"{currentDatabaseConfiguration.GetType().Name} 内部已处理，无需单独执行 SQL");
                    }
                    else
                    {
                        RecordLog(sb, "准备执行 SQL：" + backupSql);
                        int affectRows = senparcEntities.Database.ExecuteSqlRaw(backupSql);
                        RecordLog(sb, "执行完毕，备份结束。affectRows：" + affectRows);
                    }

                    if (typeParam.Options.SelectedValues.Contains($"{(int)BackupDatabaseOptions.校验备份成功}"))
                    {
                        RecordLog(sb, "检查备份文件：" + path);
                        if (File.Exists(path))
                        {
                            var modifyTime = File.GetLastWriteTimeUtc(path);
                            if ((SystemTime.UtcDateTime - modifyTime).TotalSeconds < 5/*5秒钟内创建的*/)
                            {
                                RecordLog(sb, "检查通过，备份成功！最后修改时间：" + modifyTime.ToString());
                                result.Message = "检查通过，备份成功！";
                            }
                            else
                            {
                                result.Message = $"文件存在，但修改时间不符，可能未备份成功，请检查文件！文件最后修改时间：{modifyTime.ToString()}";
                                RecordLog(sb, result.Message);
                            }
                        }
                        else
                        {
                            result.Message = "备份文件未生成，备份失败！";
                            RecordLog(sb, result.Message);
                        }
                    }
                    else
                    {
                        result.Message = "备份完成！";
                    }
                }
                catch (Exception ex)
                {
                    result.Message += ex.Message;
                    throw;
                }
            });
        }
    }
}
