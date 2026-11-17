/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysMenuService.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Areas.Admin.ACL.Repository;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysMenuService : BaseClientService<SysMenu>
    {
        private readonly ISysMenuRepository _repo;
        private readonly AdminMenuLocalizationService _menuLocalization;

        public SysMenuService(
            ISysMenuRepository repo,
            IServiceProvider serviceProvider,
            AdminMenuLocalizationService menuLocalization) : base(repo, serviceProvider)
        {
            _repo = repo;
            _menuLocalization = menuLocalization;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        private IList<SysMenuTreeItemDto> BuildTreeItems(IEnumerable<SysMenuDto> iteration, IEnumerable<SysMenuDto> source)
        {
            var items = new List<SysMenuTreeItemDto>();
            foreach (var menu in iteration)
            {
                //Legacy comment translated to English.
                var parentNode = new SysMenuTreeItemDto()
                {
                    Icon = menu.Icon,
                    Id = menu.Id,
                    IsMenu = menu.MenuType == MenuType.菜单,
                    MenuName = _menuLocalization.Resolve(menu),
                    Url = menu.Url,
                    Children = new List<SysMenuTreeItemDto>()
                };

                var children = source.Where(z => z.ParentId == menu.Id)
                                     .OrderByDescending(o => o.Sort)
                                     .ToList();
                parentNode.Children = BuildTreeItems(children, source);
                items.Add(parentNode);
            }
            return items;
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetAllMenusTreeAsync(bool hasButton)
        {
            var allMenus = (await _repo.GetAllMenuDtosAsync(hasButton))
                .Select(_menuLocalization.ResolveInPlace)
                .ToList();
            var iteration = allMenus.Where(z => z.ParentId.IsNullOrEmpty())
                                    .OrderByDescending(o => o.Sort)
                                    .ToList();
            var items = BuildTreeItems(iteration, allMenus);
            return items;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuListAsync(bool hasButton)
        {
            return (await _repo.GetAllMenuDtosAsync(hasButton))
                .Select(_menuLocalization.ResolveInPlace)
                .ToList();
        }
    }
}
