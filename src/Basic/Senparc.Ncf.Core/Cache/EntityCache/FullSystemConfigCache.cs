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
                var msg = @$"FullSystemConfigCache 访问数据库异常，推测系统未安装或未正确配置数据库。
提示信息：
{ex.Message}
{ex.StackTrace}
";
                new NcfUninstallException(msg, null);
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
                    var scheme = urlData.Scheme;//protocol
                    var host = urlData.Host.Host;//Hostname (without port)
                    var port = urlData.Host.Port ?? -1;//Port (does not use urlData.Host directly because it is ported from .NET Framework)
                    string portSetting = null;//Port part in URL
                    string schemeUpper = scheme.ToUpper();//Agreement (uppercase)
                    string baseUrl = httpContext.Request.PathBase;//Subsite application path

                    if (port == -1 || //This condition only occurs when Host.Port == null in .net core
               (schemeUpper == "HTTP" && port == 80) ||
               (schemeUpper == "HTTPS" && port == 443))
                    {
                        portSetting = "";//Use default value
                    }
                    else
                    {
                        portSetting = ":" + port;//Add port
                    }

                    hostName = $"{scheme}://{host}{portSetting}";

                }
                catch
                {
                }

                //try to install

                throw new NcfUninstallException($"NCF 系统未初始化，请先执行 {hostName}/Install 进行数据初始化", null);
            }


            base.SetData(fullSystemConfig, base.TimeOut, null);
            return base.Data;
        }
    }
}
