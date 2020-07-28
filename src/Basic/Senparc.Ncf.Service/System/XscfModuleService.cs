using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class XncfModuleService : ServiceBase<XncfModule>
    {
        public XncfModuleService(IRepositoryBase<XncfModule> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 检查并更新版本
        /// </summary>
        /// <param name="storedDto"></param>
        /// <param name="assemblyDto"></param>
        /// <returns>返回是否需要新增或更新</returns>
        public async Task<InstallOrUpdate?> CheckAndUpdateVersionAsync(CreateOrUpdate_XncfModuleDto storedDto, UpdateVersion_XncfModuleDto assemblyDto)
        {
            if (storedDto == null)
            {
                //新增模块
                var xncfModule = new XncfModule(assemblyDto.Name, assemblyDto.Uid, assemblyDto.MenuName, assemblyDto.Version, assemblyDto.Description, "", true, "", Core.Enums.XncfModules_State.新增待审核);
                xncfModule.Create();
                await base.SaveObjectAsync(xncfModule).ConfigureAwait(false);
                return InstallOrUpdate.Install;
            }
            else
            {
                //检查更新
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
        /// 跟新菜单Id
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task UpdateMenuId(UpdateMenuId_XncfModuleDto dto)
        {
            var xncfModule = await base.GetObjectAsync(z => z.Uid == dto.Uid).ConfigureAwait(false);
            xncfModule.UpdateMenuId(dto.MenuId);
            await SaveObjectAsync(xncfModule).ConfigureAwait(false);
        }
    }
}
