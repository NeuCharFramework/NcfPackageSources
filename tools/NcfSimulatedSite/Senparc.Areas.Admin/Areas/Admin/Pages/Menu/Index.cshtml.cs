using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class MenuIndexModel : BaseAdminPageModel
    {
        private readonly SysMenuService _sysMenuService;

        public MenuIndexModel(IServiceProvider serviceProvider, SysMenuService _sysMenuService)
            : base(serviceProvider)
        {
            CurrentMenu = "Menu";
            this._sysMenuService = _sysMenuService;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 
        /// </summary>
        public PagedList<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task OnGetAsync()
        {
            SysMenus = await _sysMenuService.GetObjectListAsync(PageIndex, 10, _ => true, _ => _.AddTime, Ncf.Core.Enums.OrderingType.Descending);
        }

        public IActionResult OnPostDelete(string[] ids)
        {
            foreach (var id in ids)
            {
                _sysMenuService.DeleteObject(_ => _.Id == id);
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// 配置模式：保存一级菜单拖拽排序结果，真实更新 Sort 数值
        /// </summary>
        /// <param name="request">按从上到下顺序排列的一级菜单 Id 列表</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostReorderAsync([FromBody] MenuReorderRequest request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
            {
                return Ok(false, "排序数据不能为空");
            }

            var validIds = request.Ids
                .Where(z => !string.IsNullOrEmpty(z))
                .Distinct()
                .ToList();

            // 仅处理一级菜单（ParentId 为空）
            var firstLevelMenus = await _sysMenuService.GetFullListAsync(
                _ => validIds.Contains(_.Id) && string.IsNullOrEmpty(_.ParentId));

            if (firstLevelMenus.Count == 0)
            {
                return Ok(false, "未找到可排序的一级菜单");
            }

            int total = validIds.Count;
            foreach (var menu in firstLevelMenus)
            {
                int position = validIds.IndexOf(menu.Id);
                // 左侧菜单按 Sort 降序渲染：位置越靠前（index 越小），Sort 数值越大
                int newSort = (total - position) * 10;
                if (menu.Sort != newSort)
                {
                    menu.Sort = newSort;
                    menu.LastUpdateTime = DateTime.Now;
                    await _sysMenuService.SaveObjectAsync(menu);
                }
            }

            // 刷新菜单缓存，使左侧菜单顺序立即生效
            await _sysMenuService.GetMenuDtoByCacheAsync(true);

            return Ok(true);
        }
    }

    /// <summary>
    /// 一级菜单排序请求
    /// </summary>
    public class MenuReorderRequest
    {
        public List<string> Ids { get; set; }
    }
}