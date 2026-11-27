/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysMenuAppService.cs
    File description: SysMenuAppService.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// Provides super-administrator APIs for maintaining and querying system menus.
    /// </summary>
    [BackendJwtAuthorize(BackendJwtAuthorizeAttribute.SuperAdminPolicyName)]
    public class SysMenuAppService : AppServiceBase
    {
        private readonly SysMenuService _sysMenuService;
        private readonly Domain.Services.SysMenuService _domainService;
        private readonly AutoMapper.IMapper _mapper;

        /// <summary>
        /// Initializes the menu-management application service.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="sysMenuService">The persistence service used for writes.</param>
        /// <param name="mapper">The DTO/entity mapper.</param>
        /// <param name="domainService">The domain menu query service.</param>
        public SysMenuAppService(IServiceProvider serviceProvider, SysMenuService sysMenuService, AutoMapper.IMapper mapper, Domain.Services.SysMenuService domainService) : base(serviceProvider)
        {
            _sysMenuService = sysMenuService;
            _mapper = mapper;
            _domainService = domainService;
        }

        /// <summary>
        /// Creates or updates a system menu and returns its persisted identifier.
        /// </summary>
        /// <param name="request">The menu fields to persist.</param>
        /// <returns>A response containing the menu identifier.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        [Ncf.Core.Authorization.Permission("role.add,role.update")]
        public async Task<AppResponseBase<SysMenu_CreateOrUpdateResponse>> CreateOrUpdateAsync(SysMenu_CreateOrUpdateRequest request)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_CreateOrUpdateResponse>, SysMenu_CreateOrUpdateResponse>(async (response, logger) =>
            {
                var dto = _mapper.Map<SysMenuDto>(request);
                var menu = await _sysMenuService.CreateOrUpdateAsync(dto);
                return new SysMenu_CreateOrUpdateResponse()
                {
                    Id = menu.Id
                };
            });
            return response;
        }

        /// <summary>
        /// Returns the complete menu tree, optionally including button permissions.
        /// </summary>
        /// <param name="hasButton">Whether button nodes should be included.</param>
        /// <returns>A response containing the menu tree.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_MenuTreeResponse>> GetAllMenusTreeAsync(bool hasButton)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_MenuTreeResponse>, SysMenu_MenuTreeResponse>(async (response, logger) =>
            {
                var mm = await _domainService.GetAllMenusTreeAsync(hasButton);
                return new SysMenu_MenuTreeResponse()
                {
                    Items = mm
                };
            });
            return response;
        }

        /// <summary>
        /// Returns the direct child menus of a parent identifier.
        /// </summary>
        /// <param name="parentId">The parent identifier; empty selects root menus.</param>
        /// <returns>A response containing the matching menu DTOs.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_MenusResponse>> GetMenusAsync(string parentId)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_MenusResponse>, SysMenu_MenusResponse>(async (response, logger) =>
            {
                SenparcExpressionHelper<SysMenu> helper = new SenparcExpressionHelper<SysMenu>();
                helper
                .ValueCompare
                .AndAlso(string.IsNullOrEmpty(parentId), o => o.ParentId == null || o.ParentId == string.Empty)
                .AndAlso(!string.IsNullOrEmpty(parentId), o => o.ParentId == parentId)
                ;
                var ms = await _domainService.GetFullListAsync(helper.BuildWhereExpression(), "Sort DESC");
                var items = _mapper.Map<IEnumerable<SysMenuDto>>(ms.AsEnumerable());
                return new SysMenu_MenusResponse()
                {
                    Items = items
                };
            });
            return response;
        }

        /// <summary>
        /// Gets one menu by its identifier.
        /// </summary>
        /// <param name="id">The menu identifier.</param>
        /// <returns>A response containing the mapped menu DTO.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SysMenu_GetMenuResponse>> GetMenuAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_GetMenuResponse>, SysMenu_GetMenuResponse>(async (response, logger) =>
            {
                var ms = await _domainService.GetObjectAsync(o => o.Id == id);
                return new SysMenu_GetMenuResponse()
                {
                    Item = _mapper.Map<SysMenuDto>(ms)
                };
            });
            return response;
        }

        /// <summary>
        /// Returns all menus, optionally including button permissions.
        /// </summary>
        /// <param name="hasButton">Whether button data should be included.</param>
        /// <returns>A response containing the complete menu list.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<List<SysMenuDto>>> GetAllMenuListAsync(bool hasButton)
        {
            var response = await this.GetResponseAsync<AppResponseBase<List<SysMenuDto>>, List<SysMenuDto>>(async (response, logger) =>
            {
                var menuList = await _domainService.GetAllMenuListAsync(hasButton);
                return menuList.ToList();
            });

            return response;
        }

        /// <summary>
        /// Deletes a menu and returns the deleted menu representation.
        /// </summary>
        /// <param name="id">The menu identifier to delete.</param>
        /// <returns>A response containing the deleted menu DTO.</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<SysMenu_GetMenuResponse>> DeleteMenuAsync(string id)
        {
            var response = await this.GetResponseAsync<AppResponseBase<SysMenu_GetMenuResponse>, SysMenu_GetMenuResponse>(async (response, logger) =>
            {
                var ms = await _domainService.GetObjectAsync(o => o.Id == id);
                await _domainService.DeleteObjectAsync(ms);
                return new SysMenu_GetMenuResponse()
                {
                    Item = _mapper.Map<SysMenuDto>(ms)
                };
            });
            return response;
        }
    }
}
