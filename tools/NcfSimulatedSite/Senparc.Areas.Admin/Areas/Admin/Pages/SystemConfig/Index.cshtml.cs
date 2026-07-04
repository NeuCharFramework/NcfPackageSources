using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.SystemManager.Domain.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class SystemConfig_IndexModel(IServiceProvider serviceProvider,
        SystemConfigService systemConfigService)
        : BaseAdminPageModel(serviceProvider)
    {
        private const int MinExpireMinutes = 5;
        private const int MaxExpireMinutes = 60 * 24 * 365; // 1 year

        private readonly SystemConfigService _systemConfigService = systemConfigService;
        public async Task<IActionResult> OnGetAsync()
        {
            await Task.CompletedTask;
            return Page();
        }

        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            var systemConfig = await _systemConfigService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);
            return Ok(new { List = systemConfig.AsEnumerable() });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] FullSystemConfig fullSystemConfig)
        {
            if (fullSystemConfig == null)
            {
                return Ok(false, "请求参数不能为空");
            }

            if (string.IsNullOrWhiteSpace(fullSystemConfig.SystemName))
            {
                return Ok(false, "系统名称不能为空");
            }

            if (fullSystemConfig.AdminWebLoginExpireMinutes < MinExpireMinutes || fullSystemConfig.AdminWebLoginExpireMinutes > MaxExpireMinutes)
            {
                return Ok(false, $"网页登录持续时间必须在 {MinExpireMinutes}-{MaxExpireMinutes} 分钟范围内");
            }

            if (fullSystemConfig.BackendJwtExpireMinutes < MinExpireMinutes || fullSystemConfig.BackendJwtExpireMinutes > MaxExpireMinutes)
            {
                return Ok(false, $"JWT 过期时间必须在 {MinExpireMinutes}-{MaxExpireMinutes} 分钟范围内");
            }

            var systemConfig = await _systemConfigService.GetObjectAsync(z => true);
            systemConfig.Update(fullSystemConfig.SystemName,
                systemConfig.MchId,
                systemConfig.MchKey,
                systemConfig.TenPayAppId,
                systemConfig.HideModuleManager,
                fullSystemConfig.AdminWebLoginExpireMinutes,
                fullSystemConfig.BackendJwtExpireMinutes);

            await _systemConfigService.SaveObjectAsync(systemConfig);

            base.SetMessager(MessageType.success, "修改成功");
            return Ok(new
            {
                systemName = systemConfig.SystemName,
                adminWebLoginExpireMinutes = systemConfig.AdminWebLoginExpireMinutes,
                backendJwtExpireMinutes = systemConfig.BackendJwtExpireMinutes
            });
        }
    }
}
