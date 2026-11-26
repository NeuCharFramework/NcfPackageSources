using Microsoft.AspNetCore.Http;
using Senparc.Areas.Admin.Domain.Models.Dto;
using Senparc.CO2NET;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.Installer.Domain;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Instraller.Pages;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer.OHS.Local.AppService
{
    public class InstallAppService : AppServiceBase
    {
        private readonly InstallerService _installerService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly Lazy<IHttpContextAccessor> _httpContextAccessor;

        /// <summary>
        /// Initializes the application-service facade for installer and tenant bootstrap operations.
        /// </summary>
        /// <param name="serviceProvider">The provider used by the base application service.</param>
        /// <param name="installerService">The service that executes the installation workflow.</param>
        /// <param name="tenantInfoService">The service that creates and reads tenant information.</param>
        /// <param name="httpContextAccessor">Lazy access to the current HTTP context.</param>
        public InstallAppService(IServiceProvider serviceProvider, InstallerService installerService, TenantInfoService tenantInfoService, Lazy<IHttpContextAccessor> httpContextAccessor) : base(serviceProvider)
        {
            this._installerService = installerService;
            this._tenantInfoService = tenantInfoService;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Executes the submitted system installation request and wraps its status
        /// in the standard application response contract.
        /// </summary>
        /// <param name="installRequestDto">The database, system, administrator, and module options.</param>
        /// <returns>The installation response.</returns>
        [ApiBind()]
        public async Task<AppResponseBase<InstallResponseDto>> InstallAsync(InstallRequestDto installRequestDto)
        {
            return await this.GetResponseAsync<AppResponseBase<InstallResponseDto>, InstallResponseDto>(async (response, logger) =>
            {
                return await _installerService.InstallAsync(installRequestDto, base.ServiceProvider);
            });
        }

        /// <summary>
        /// Returns the default installation options currently available to the wizard.
        /// </summary>
        /// <returns>The default database, system, administrator, and module options.</returns>
        public async Task<AppResponseBase<GetDefaultInstallOptionsResponseDto>> GetInstallOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetDefaultInstallOptionsResponseDto>, GetDefaultInstallOptionsResponseDto>(async (response, logger) =>
            {
                return  _installerService.GetDefaultInstallOptions();
            });
        }

        /// <summary>
        /// Creates or loads the current request tenant context and returns its DTO.
        /// </summary>
        /// <returns>The current tenant information response.</returns>
        public async Task<AppResponseBase<TenantInfoDto>> GetTenantInfoAsync()
        {
            var httpContext = _httpContextAccessor.Value.HttpContext;
            var tenantInfo = await _tenantInfoService.CreateInitTenantInfoAsync(httpContext);

            //Legacy comment translated to English.
            var createdRequestTenantInfo = await _tenantInfoService.SetScopedRequestTenantInfoAsync(httpContext);

            return await this.GetResponseAsync<AppResponseBase<TenantInfoDto>, TenantInfoDto>(async (response, logger) =>
            {
                return _tenantInfoService.Mapper.Map<TenantInfoDto>(await _tenantInfoService.GetObjectAsync(z => z.Id == createdRequestTenantInfo.Id));
            });
        }
    }
}
