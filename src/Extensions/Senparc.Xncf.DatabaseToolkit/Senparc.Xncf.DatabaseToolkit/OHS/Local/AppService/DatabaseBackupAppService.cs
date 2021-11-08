using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.DatabaseToolkit.Domain.Services;
using Senparc.Xncf.DatabaseToolkit.OHS.Local.PL;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.AppService
{
    public class DatabaseBackupAppService : AppServiceBase
    {
        private readonly DbConfigQueryService _dbConfigQueryService;

        public DatabaseBackupAppService(IServiceProvider serviceProvider, DbConfigQueryService dbConfigQueryService) : base(serviceProvider)
        {
            this._dbConfigQueryService = dbConfigQueryService;
        }

        //[ApiBind("A","B",ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        [FunctionRender("检查自动备份状态", "必须已经设置过自动配分时间，且大于 0 才能启用自动备份", typeof(Register))]
        public async Task<DatabaseAutoBackup_IsAutoBackupResponse> IsAutoBackup()
        {
            return await this.GetResponseAsync<DatabaseAutoBackup_IsAutoBackupResponse, bool>(async (response, logger) =>
                {
                    return await _dbConfigQueryService.IsAutoBackup();
                });
        }

        #region 备份

        [FunctionRender("备份数据库", "将当前使用的数据库备份到指定路径。友情提示：建议确保该路径不具备公开访问权限！", typeof(Register))]
        public async Task<StringAppResponse> Backup(DatabaseBackup_BackupRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                try
                {
                    var path = request.Path;

                    if (File.Exists(path))
                    {
                        var copyPath = path + ".last.bak";
                        if (request.Options.SelectedValues.Contains($"{(int)BackupDatabaseOptions.如果文件存在则不覆盖}"))
                        {
                            logger.Append("检测到同名文件，停止覆盖。地址：" + copyPath);
                            return response.Data = logger.Append("检测到同名文件，停止覆盖！");
                        }

                        logger.Append("检测到同名文件，已经移动到（并覆盖）：" + copyPath);
                        File.Move(path, copyPath);
                    }

                    logger.Append("开始获取 ISenparcEntities 对象");
                    var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntitiesDbContext)) as SenparcEntitiesBase;
                    logger.Append("获取 ISenparcEntities 对象成功");

                    var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.Current;
                    var backupSql = currentDatabaseConfiguration.GetBackupDatabaseSql(senparcEntities.Database.GetDbConnection(), path);
                    if (backupSql.IsNullOrEmpty())
                    {
                        logger.Append($"{currentDatabaseConfiguration.GetType().Name} 内部已处理，无需单独执行 SQL");
                    }
                    else
                    {
                        logger.Append("准备执行 SQL：" + backupSql);
                        int affectRows = senparcEntities.Database.ExecuteSqlRaw(backupSql);
                        logger.Append("执行完毕，备份结束。affectRows：" + affectRows);
                    }

                    if (request.Options.SelectedValues.Contains($"{(int)BackupDatabaseOptions.校验备份成功}"))
                    {
                        logger.Append("检查备份文件：" + path);
                        if (File.Exists(path))
                        {
                            var modifyTime = File.GetLastWriteTimeUtc(path);
                            if ((SystemTime.UtcDateTime - modifyTime).TotalSeconds < 5/*5秒钟内创建的*/)
                            {
                                logger.Append("检查通过，备份成功！最后修改时间：" + modifyTime.ToString());
                                response.Data = "检查通过，备份成功！";
                            }
                            else
                            {
                                response.Data = logger.Append($"文件存在，但修改时间不符，可能未备份成功，请检查文件！文件最后修改时间：{modifyTime.ToString()}");
                            }
                        }
                        else
                        {
                            response.Data = logger.Append($"备份文件未生成，备份失败！");
                        }
                    }
                    else
                    {
                        response.Data = "备份完成！";
                    }
                    return "2222";
                }
                catch (Exception ex)
                {
                    throw;
                }
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append(ex.Message);
                logger.Append(ex.StackTrace);
            },
            saveLogAfterFinished: true,
            saveLogName: "数据库备份");
        }

        #endregion


        [FunctionRender("导出当前数据库 SQL 脚本", "导出当前站点正在使用的所有表的 SQL 脚本", typeof(Register))]
        public async Task<StringAppResponse> ExportSQL()
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                logger.Append("开始获取 ISenparcEntities 对象");
                var senparcEntities = ServiceProvider.GetService(typeof(ISenparcEntitiesDbContext)) as SenparcEntitiesBase;
                logger.Append("获取 ISenparcEntities 对象成功");
                logger.Append("开始生成 SQL ");
                var sql = senparcEntities.Database.GenerateCreateScript();
                logger.Append("SQL 已生成：");
                logger.Append($"============ NCF Database {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  ============");
                logger.Append("\r\n\r\n" + sql + "\r\n\r\n");
                logger.Append($"============ NCF Database {SystemTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  ============");
                return logger.Append("SQL 已生成，请下载日志文件！");
            });
        }
    }
}
