/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenuService.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
        /// Recursively converts flat menu DTOs into localized hierarchical menu items.
        /// Children are ordered by descending stored sort value.
        /// </summary>
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
        /// Loads all menus, localizes their display metadata, and builds the root menu hierarchy.
        /// </summary>
        /// <param name="hasButton">Whether button definitions should be included in the source query.</param>
        /// <returns>The localized hierarchical menu items.</returns>
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
        /// Loads all menu DTOs and applies current localization metadata without building a hierarchy.
        /// </summary>
        /// <param name="hasButton">Whether button definitions should be included in the source query.</param>
        /// <returns>The localized flat menu DTO collection.</returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuListAsync(bool hasButton)
        {
            return (await _repo.GetAllMenuDtosAsync(hasButton))
                .Select(_menuLocalization.ResolveInPlace)
                .ToList();
        }
    }
}
