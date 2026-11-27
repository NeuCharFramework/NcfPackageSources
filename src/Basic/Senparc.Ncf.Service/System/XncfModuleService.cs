/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleService.cs
    File description: XncfModuleService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.23.13.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Ncf.Service
{
    public class XncfModuleService : ServiceBase<XncfModule>
    {
        /// <summary>
        /// Initializes the XNCF module service.
        /// </summary>
        /// <param name="repo">The repository for module entities.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public XncfModuleService(IRepositoryBase<XncfModule> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Compares persisted module metadata with the discovered assembly registration,
        /// creating a new module record or updating its version metadata when needed.
        /// </summary>
        /// <param name="storedDto">The persisted module DTO, or <see langword="null"/> when not installed.</param>
        /// <param name="assemblyDto">The registration metadata discovered from the assembly.</param>
        /// <returns><see cref="InstallOrUpdate.Install"/> for a new module, <see cref="InstallOrUpdate.Update"/> for a version change, or <see langword="null"/> when unchanged.</returns>
        public async Task<InstallOrUpdate?> CheckAndUpdateVersionAsync(CreateOrUpdate_XncfModuleDto storedDto, UpdateVersion_XncfModuleDto assemblyDto)
        {
            if (storedDto == null)
            {
                //Legacy comment translated to English.
                var xncfModule = new XncfModule(assemblyDto.Name, assemblyDto.Uid, assemblyDto.MenuName, assemblyDto.Version, assemblyDto.Description, "", true, "", assemblyDto.Icon, Core.Enums.XncfModules_State.新增待审核);
                xncfModule.Create();
                await base.SaveObjectAsync(xncfModule).ConfigureAwait(false);
                return InstallOrUpdate.Install;
            }
            else
            {
                //checkupdate
                if (storedDto.Version != assemblyDto.Version)
                {
                    var xncfModule = base.GetObject(z => z.Uid == storedDto.Uid);
                    xncfModule.UpdateVersion(assemblyDto.Version, assemblyDto.MenuName, assemblyDto.Description);
                    await base.SaveObjectAsync(xncfModule).ConfigureAwait(false);
                    return InstallOrUpdate.Update;
                }
                return null;
            }
        }

        /// <summary>
        /// Updates the persisted administration menu identifier for a module.
        /// </summary>
        /// <param name="dto">The module UID and menu identifier.</param>
        /// <exception cref="InvalidOperationException">Thrown when the module record cannot be found.</exception>
        public async Task UpdateMenuIdAsync(UpdateMenuId_XncfModuleDto dto)
        {
            var xncfModule = await base.GetObjectAsync(z => z.Uid == dto.Uid).ConfigureAwait(false);
            xncfModule.UpdateMenuId(dto.MenuId);
            await SaveObjectAsync(xncfModule).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override void SaveObject(XncfModule obj)
        {
            var isInsert = base.IsInsert(obj);
            if (isInsert)
            {
                obj.Flag = false;
            }
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("XncfModule{2}：{0}（ID：{1}）", obj.Name, obj.Id, isInsert ? "新增" : "编辑");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullXncfModuleCache>();
            //Legacy comment translated to English.
            using (fullUserCache.Cache.BeginCacheLock(FullXncfModuleCache.CACHE_KEY, obj.Id.ToString()))
            {
                fullUserCache.RemoveObject(obj.Name);
            }
        }

        /// <inheritdoc />
        public override async Task SaveObjectAsync(XncfModule obj)
        {
            var isInsert = base.IsInsert(obj);
            if (isInsert)
            {
                obj.Flag = false;
            }
            await base.SaveObjectAsync(obj);
            LogUtility.WebLogger.InfoFormat("XncfModule{2}：{0}（ID：{1}）", obj.Name, obj.Id, isInsert ? "新增" : "编辑");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullXncfModuleCache>();
            //Legacy comment translated to English.
            using (await fullUserCache.Cache.BeginCacheLockAsync(FullXncfModuleCache.CACHE_KEY, obj.Id.ToString()).ConfigureAwait(false))
            {
                await fullUserCache.RemoveObjectAsync(obj.Name);
            }
        }

        /// <inheritdoc />
        public override void DeleteObject(XncfModule obj)
        {
            obj.Flag = true;
            base.SaveObject(obj);
            LogUtility.WebLogger.Info($"XncfModule被删除：{obj.Name}（ID：{obj.Id}）");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullXncfModuleCache>();
            //Legacy comment translated to English.
            using (fullUserCache.Cache.BeginCacheLock(FullXncfModuleCache.CACHE_KEY, obj.Id.ToString()))
            {
                fullUserCache.RemoveObject(obj.Name);
            }
        }

        /// <inheritdoc />
        public override async Task DeleteObjectAsync(XncfModule obj)
        {
            obj.Flag = true;
            await base.SaveObjectAsync(obj).ConfigureAwait(false);
            LogUtility.WebLogger.Info($"XncfModule被删除：{obj.Name}（ID：{obj.Id}）");

            //clear the cache
            var fullUserCache = _serviceProvider.GetService<FullXncfModuleCache>();
            //Legacy comment translated to English.
            using (await fullUserCache.Cache.BeginCacheLockAsync(FullXncfModuleCache.CACHE_KEY, obj.Id.ToString()).ConfigureAwait(false))
            {
                await fullUserCache.RemoveObjectAsync(obj.Name);
            }
        }
    }
}
