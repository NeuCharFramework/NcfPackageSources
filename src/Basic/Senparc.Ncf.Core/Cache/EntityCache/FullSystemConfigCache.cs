/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullSystemConfigCache.cs
    File description: FullSystemConfigCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Ncf.Core.Cache
{
    [AutoDIType(DILifecycleType.Scoped)]
    public class FullSystemConfigCache : BaseCache<FullSystemConfig>/*, IFullSystemConfigCache*/
    {
        public const string CACHE_KEY = "FullSystemConfigCache";
        private INcfDbData _dataContext => base._db as INcfDbData;

        public FullSystemConfigCache(INcfDbData db)
            : base(CACHE_KEY, db)
        {
            base.TimeOut = 1440;
        }

        public override FullSystemConfig Update()
        {
            SystemConfig systemConfig = null;
            try
            {
                systemConfig = (_dataContext.BaseDataContext as SenparcEntitiesBase).Set<SystemConfig>().FirstOrDefault();
            }
            catch (Exception ex)
            {
                var msg = @$"FullSystemConfigCache 访问数据库异常，可能是首次安装、架构待升级或数据库连接不可用。
提示信息：
{ex.Message}
{ex.StackTrace}
";
                if (DatabaseInstallState.IsSchemaUpgradeRequired(ex))
                {
                    throw new NcfDatabaseUpgradeRequiredException(msg, ex);
                }

                if (DatabaseInstallState.IsDatabaseUnavailableForInstallation(ex))
                {
                    throw new NcfUninstallException(msg, ex);
                }

                throw new NcfDatabaseException(msg, null, _dataContext.BaseDataContext?.GetType(), ex);
            }

            FullSystemConfig fullSystemConfig;
            if (systemConfig != null)
            {
                fullSystemConfig = FullSystemConfig.CreateEntity<FullSystemConfig>(systemConfig);
            }
            else
            {
                string hostName = null;
                try
                {
                    var httpContextAccessor = SenparcDI.GetServiceProvider().GetService<IHttpContextAccessor>();
                    var httpContext = httpContextAccessor.HttpContext;
                    var urlData = httpContext.Request;
                    var scheme = urlData.Scheme;//Legacy comment translated to English.
                    var host = urlData.Host.Host;//Legacy comment translated to English.
                    var port = urlData.Host.Port ?? -1;//Legacy comment translated to English.
                    string portSetting = null;//Legacy comment translated to English.
                    string schemeUpper = scheme.ToUpper();//Legacy comment translated to English.
                    string baseUrl = httpContext.Request.PathBase;//Legacy comment translated to English.

                    if (port == -1 || //Legacy comment translated to English.
               (schemeUpper == "HTTP" && port == 80) ||
               (schemeUpper == "HTTPS" && port == 443))
                    {
                        portSetting = "";//Legacy comment translated to English.
                    }
                    else
                    {
                        portSetting = ":" + port;//Legacy comment translated to English.
                    }

                    hostName = $"{scheme}://{host}{portSetting}";

                }
                catch
                {
                }

                //Legacy comment translated to English.

                throw new NcfUninstallException($"NCF 系统未初始化，请先执行 {hostName}/Install 进行数据初始化", null);
            }


            base.SetData(fullSystemConfig, base.TimeOut, null);
            return base.Data;
        }
    }
}
