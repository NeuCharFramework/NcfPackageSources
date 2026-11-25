/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemConfigServiceBase.cs
    File description: SystemConfigServiceBase related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class SystemConfigServiceBase : ClientServiceBase<SystemConfig>
    {
        /// <summary>
        /// Initializes the system-configuration service.
        /// </summary>
        /// <param name="repo">Repository for system configuration.</param>
        /// <param name="serviceProvider">Dependency-injection provider.</param>
        public SystemConfigServiceBase(IClientRepositoryBase<SystemConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <inheritdoc />
        public override void SaveObject(SystemConfig obj)
        {
            LogUtility.SystemLogger.Info("系统信息被编辑");

            base.SaveObject(obj);

            //Legacy comment translated to English.
            var systemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            systemConfigCache.RemoveCache();
        }

        /// <inheritdoc />
        public override async Task SaveObjectAsync(SystemConfig obj)
        {
            LogUtility.SystemLogger.Info("系统信息被编辑");

            await base.SaveObjectAsync(obj);

            //Legacy comment translated to English.
            var systemConfigCache = _serviceProvider.GetService<FullSystemConfigCache>();
            await systemConfigCache.RemoveCacheAsync();
            await base.SaveObjectAsync(obj);
        }

        /// <summary>
        /// Returns a timestamp token used by legacy database-backup workflows.
        /// </summary>
        /// <returns>A local-time timestamp formatted as <c>yyyyMMdd-HH-mm</c>.</returns>
        public virtual string BackupDatabase()
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMdd-HH-mm");//Legacy comment translated to English.
            return timeStamp;
        }

        /// <summary>
        /// Provides the legacy application-pool recycle hook; the base implementation performs no action.
        /// </summary>
        public virtual void RecycleAppPool()
        {
            //string webConfigPath = HttpContext.Current.Server.MapPath("~/Web.config");
            //System.IO.File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
        }

        /// <inheritdoc />
        /// <exception cref="Exception">Always thrown because system configuration cannot be deleted.</exception>
        public override void DeleteObject(SystemConfig obj)
        {
            throw new Exception("系统信息不能被删除！");
        }
    }
}
