/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related feature implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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

            if (validIds.Count == 0)
            {
                return Ok(false, "排序数据不能为空");
            }

            var menus = (await _sysMenuService.GetFullListAsync(_ => validIds.Contains(_.Id))).ToList();
            if (menus.Count == 0)
            {
                return Ok(false, "未找到可排序的菜单");
            }

            //Legacy comment translated to English.
            var sortableMenus = menus
                .Where(_ => _.MenuType == MenuType.菜单 || _.MenuType == MenuType.页面)
                .ToList();

            //Legacy comment translated to English.
            var parentGroups = new Dictionary<string, List<(int Index, SysMenu Menu)>>();
            foreach (var menu in sortableMenus)
            {
                var parentKey = menu.ParentId ?? string.Empty;
                if (!parentGroups.TryGetValue(parentKey, out var group))
                {
                    group = new List<(int, SysMenu)>();
                    parentGroups[parentKey] = group;
                }
                group.Add((validIds.IndexOf(menu.Id), menu));
            }

            var toUpdate = new List<SysMenu>();
            foreach (var parentGroup in parentGroups)
            {
                var ordered = parentGroup.Value.OrderBy(z => z.Index).ToList();
                int total = ordered.Count;
                for (int i = 0; i < ordered.Count; i++)
                {
                    var menu = ordered[i].Menu;
                    //Legacy comment translated to English.
                    int newSort = (total - i) * 10;
                    if (menu.Sort != newSort)
                    {
                        menu.Sort = newSort;
                        menu.LastUpdateTime = DateTime.Now;
                        toUpdate.Add(menu);
                    }
                }
            }

            if (toUpdate.Count > 0)
            {
                await _sysMenuService.SaveObjectListAsync(toUpdate);
            }

            //Legacy comment translated to English.
            await _sysMenuService.GetMenuDtoByCacheAsync(true);

            return Ok(true);
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class MenuReorderRequest
    {
        public List<string> Ids { get; set; }
    }
}