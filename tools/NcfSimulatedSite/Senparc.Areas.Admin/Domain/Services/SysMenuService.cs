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
    /// menu
    /// </summary>
    public class SysMenuService : BaseClientService<SysMenu>
    {
        private readonly ISysMenuRepository _repo;
        public SysMenuService(ISysMenuRepository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _repo = repo;
        }

        /// <summary>
        /// Recursively obtain the tree structure
        /// </summary>
        /// <param name="iteration">Current filter list</param>
        /// <param name="source">Full list data</param>
        private IList<SysMenuTreeItemDto> BuildTreeItems(IEnumerable<SysMenuDto> iteration, IEnumerable<SysMenuDto> source)
        {
            var items = new List<SysMenuTreeItemDto>();
            foreach (var menu in iteration)
            {
                //Find submenu
                var parentNode = new SysMenuTreeItemDto()
                {
                    Icon = menu.Icon,
                    Id = menu.Id,
                    IsMenu = menu.MenuType == MenuType.菜单,
                    MenuName = menu.MenuName,
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
        /// Get all menus (excluding pages)
        ///TODO... cache
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuTreeItemDto>> GetAllMenusTreeAsync(bool hasButton)
        {
            var allMenus = await _repo.GetAllMenuDtosAsync(hasButton);
            var iteration = allMenus.Where(z => z.ParentId.IsNullOrEmpty())
                                    .OrderByDescending(o => o.Sort)
                                    .ToList();
            var items = BuildTreeItems(iteration, allMenus);
            return items;
        }

        /// <summary>
        /// Get complete menu information
        /// </summary>
        /// <param name="hasButton">Whether to include button information</param>
        /// <returns></returns>
        public async Task<IEnumerable<SysMenuDto>> GetAllMenuListAsync(bool hasButton)
        {
            return await _repo.GetAllMenuDtosAsync(hasButton);
        }
    }
}
