/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml.cs implementation and project behavior.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Pipelines.Sockets.Unofficial.Buffers;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    public class TenantInfo_IndexModel(IServiceProvider serviceProvider, TenantInfoService tenantInfoService,
            InstallerService installerService,
            /*, FullSystemConfigCache fullSystemConfigCache*/ AdminUserInfoService adminUserInfoService)
            : BaseAdminPageModel(serviceProvider)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly TenantInfoService _tenantInfoService = tenantInfoService;
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
        private readonly IStringLocalizer<AdminResource> _localizer = serviceProvider.GetRequiredService<IStringLocalizer<AdminResource>>();
        /// <summary>
        /// Determines whether the current request is running in the platform-tenant
        /// context allowed to manage tenant definitions.
        /// </summary>
        private bool IsPlatformAdmin()
        {
            if (!SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                return true;
            }

            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            return requestTenantInfo.MatchSuccess && requestTenantInfo.Id == 0;
        }

        private IActionResult PlatformAdminOnlyResult()
        {
            return Ok(false, _localizer["Tenant.PlatformAdminOnly"]);
        }

        /// <summary>Loads the tenant administration page.</summary>
        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

        /// <summary>Returns the current request tenant and active tenant settings.</summary>
        public async Task<IActionResult> OnGetRequestTenantInfoAsync()
        {
            if (!IsPlatformAdmin())
            {
                return PlatformAdminOnlyResult();
            }

            var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
            return Ok(new
            {
                requestTenantInfo = new
                {
                    requestTenantInfo.Id,
                    requestTenantInfo.Name,
                    requestTenantInfo.TenantKey,
                    BeginTime = requestTenantInfo.BeginTime.ToString("G"),
                },
                tenantRule = SiteConfig.SenparcCoreSetting.TenantRule.ToString(),
                SiteConfig.SenparcCoreSetting.EnableMultiTenant
            }); ;
        }

        /// <summary>Returns a paged tenant list for the platform administrator.</summary>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageSize">The maximum number of tenants per page.</param>
        /// <returns>An HTTP 200 JSON result containing tenant rows and paging metadata.</returns>
        //[Ncf.AreaBase.Admin.Filters.CustomerResource("admin-get-systemconfig")]
        public async Task<IActionResult> OnGetListAsync(int pageIndex, int pageSize)
        {
            if (!IsPlatformAdmin())
            {
                return PlatformAdminOnlyResult();
            }

            var tenantInfo = await _tenantInfoService.GetObjectListAsync(pageIndex, pageSize, z => true, z => z.Id, OrderingType.Ascending);

            var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
            List<Dictionary<string, object>> rows = null;
            if (enableMultiTenant)
            {
                //Legacy comment translated to English.
                rows = tenantInfo.AsEnumerable().Select(z => new Dictionary<string, object>
                {
                    ["id"] = z.Id,
                    ["name"] = z.Name,
                    ["tenantKey"] = z.TenantKey,
                    ["adminRemark"] = z.AdminRemark,
                    ["enable"] = z.Enable,
                    ["addTime"] = z.AddTime,
                    ["lastUpdateTime"] = z.LastUpdateTime,
                    ["adminUserCount"] = 0
                }).ToList();

                foreach (var row in rows)
                {
                    row["adminUserCount"] = await _adminUserInfoService.GetTenantUserCountAsync((int)row["id"]);
                }
            }

            return Ok(new
            {
                List = rows ?? tenantInfo.AsEnumerable().Select(z => new Dictionary<string, object>
                {
                    ["id"] = z.Id,
                    ["name"] = z.Name,
                    ["tenantKey"] = z.TenantKey,
                    ["adminRemark"] = z.AdminRemark,
                    ["enable"] = z.Enable,
                    ["addTime"] = z.AddTime,
                    ["lastUpdateTime"] = z.LastUpdateTime,
                    ["adminUserCount"] = 0
                }),
                TotalCount = tenantInfo.TotalCount,
                PageIndex = tenantInfo.PageIndex,
                enableMultiTenant
            });
        }

        /// <summary>Creates or updates a tenant after validating names and keys.</summary>
        /// <param name="dto">The tenant fields to persist.</param>
        /// <returns>An HTTP result describing validation or persistence success.</returns>
        [CustomerResource("admin-add", "admin-edit")]
        public async Task<IActionResult> OnPostSaveAsync([FromBody] CreateOrUpdate_TenantInfoDto dto)
        {
            if (!IsPlatformAdmin())
            {
                return PlatformAdminOnlyResult();
            }

            if (!ModelState.IsValid)
            {
                return Ok(false);
            }
            bool isNameExists = await this._tenantInfoService.CheckNameExisted(dto.Id, dto.Name);
            if (isNameExists)
            {
                return Ok(false, _localizer["Tenant.NameExists", dto.Name]);
            }

            bool isTenantKeyExists = await this._tenantInfoService.CheckTenantKeyExisted(dto.Id, dto.TenantKey);
            if (isTenantKeyExists)
            {
                return Ok(false, _localizer["Tenant.KeyExists", dto.TenantKey]);
            }

            try
            {
                await _tenantInfoService.CreateOrUpdateTenantInfoAsync(dto);
                return Ok(true, dto.Id > 0 ? _localizer["Tenant.SaveSuccess.Edit"] : _localizer["Tenant.SaveSuccess.Create"]);
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }

        }

        /// <summary>Initializes a tenant's system configuration and first administrator.</summary>
        /// <param name="dto">The tenant and administrator initialization values.</param>
        /// <returns>An HTTP result containing initialization credentials when successful.</returns>
        [CustomerResource("admin-add")]
        public async Task<IActionResult> OnPostInitializeAsync([FromBody] TenantInitializeDto dto)
        {
            if (!IsPlatformAdmin())
            {
                return PlatformAdminOnlyResult();
            }

            if (!ModelState.IsValid)
            {
                return Ok(false);
            }

            try
            {
                var tenantId = dto.TenantId;
                var tenantInfo = await _tenantInfoService.GetObjectAsync(z => z.Id == tenantId);
                if (tenantInfo == null)
                {
                    return Ok(false, _localizer["Tenant.NotFound"]);
                }

                //Legacy comment translated to English.
                ISenparcEntitiesDbContext senparcDB = _adminUserInfoService.BaseData.BaseDB.BaseDataContext as ISenparcEntitiesDbContext;
                if (senparcDB == null)
                {
                    return Ok(false, _localizer["Tenant.SetInfoFailed"]);
                }

                senparcDB.TenantInfo = new RequestTenantInfo()
                {
                    Id = tenantInfo.Id,
                    Name = tenantInfo.Name,
                    TenantKey = tenantInfo.TenantKey,
                };
                senparcDB.TenantInfo.TryMatch(true);

                var adminUserInfoResult = await _adminUserInfoService.InitAsync(dto.AdminAccount);

                var adminUserInfo = adminUserInfoResult.AdminUserInfo;

                await installerService.InitSystemAsync(dto.SystemName, adminUserInfo.Id, tenantInfo);

                return Ok(new
                {
                    tenantInfo = new
                    {
                        id = tenantInfo.Id,
                        name = tenantInfo.Name,
                        tenantKey = tenantInfo.TenantKey
                    },
                    adminAccount = new
                    {
                        username = dto.AdminAccount,
                        password = adminUserInfoResult.Password
                    }
                }, true, _localizer["Tenant.InitializeSuccess"]);
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }
        }

        /// <summary>
        /// Deletes tenants after checking current-tenant, last-enabled-tenant,
        /// and administrator-user safety constraints.
        /// </summary>
        /// <param name="ids">The tenant identifiers selected for deletion.</param>
        /// <returns>An HTTP result describing the deletion outcome.</returns>
        [CustomerResource("admin-delete")]
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] List<int> ids)
        {
            if (!IsPlatformAdmin())
            {
                return PlatformAdminOnlyResult();
            }

            if (ids == null || ids.Count == 0)
            {
                return Ok(false);
            }

            try
            {
                var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;
                var requestTenantInfo = _serviceProvider.GetRequiredService<RequestTenantInfo>();
                var currentTenantId = enableMultiTenant && requestTenantInfo.MatchSuccess ? requestTenantInfo.Id : 0;

                var allTenants = (await _tenantInfoService.GetFullListAsync(z => true)).ToList();
                var enabledCount = allTenants.Count(z => z.Enable);

                foreach (var id in ids)
                {
                    var tenantInfo = allTenants.FirstOrDefault(z => z.Id == id);
                    if (tenantInfo == null)
                    {
                        return Ok(false, _localizer["Tenant.DeleteNotFound", id]);
                    }

                    if (enableMultiTenant && currentTenantId > 0 && id == currentTenantId)
                    {
                        return Ok(false, _localizer["Tenant.DeleteCurrentTenantNotAllowed"]);
                    }

                    if (tenantInfo.Enable && enabledCount <= 1)
                    {
                        return Ok(false, _localizer["Tenant.DeleteLastTenantNotAllowed"]);
                    }

                    var adminUserCount = await _adminUserInfoService.GetTenantUserCountAsync(id);
                    if (adminUserCount > 0)
                    {
                        return Ok(false, _localizer["Tenant.DeleteHasAdminUsers", tenantInfo.Name, adminUserCount]);
                    }

                    await _tenantInfoService.DeleteObjectAsync(tenantInfo);
                    if (tenantInfo.Enable)
                    {
                        enabledCount--;
                    }
                    await tenantInfo.ClearCache(_serviceProvider);
                }

                return Ok(true, _localizer["Tenant.DeleteSuccess"]);
            }
            catch (Exception ex)
            {
                return Ok(false, ex.Message);
            }
        }
    }

    public class TenantInitializeDto
    {
        public int TenantId { get; set; }
        public string SystemName { get; set; }
        public string AdminAccount { get; set; }
    }
}
