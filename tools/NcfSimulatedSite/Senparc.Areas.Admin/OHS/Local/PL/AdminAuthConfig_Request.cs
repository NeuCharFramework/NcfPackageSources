using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.OHS.PL
{
    public class AdminAuthConfig_SetExpireSettingsRequest : FunctionAppRequestBase
    {
        [Range(5, 525600)]
        public int AdminWebLoginExpireMinutes { get; set; } = AdminAuthConfig.DefaultAdminWebLoginExpireMinutes;

        [Range(5, 525600)]
        public int BackendJwtExpireMinutes { get; set; } = AdminAuthConfig.DefaultBackendJwtExpireMinutes;
    }
}
