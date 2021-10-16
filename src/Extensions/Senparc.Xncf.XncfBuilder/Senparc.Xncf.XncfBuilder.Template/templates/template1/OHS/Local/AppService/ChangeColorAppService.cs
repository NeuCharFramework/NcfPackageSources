using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Template_OrgName.Xncf.Template_XncfName.Domain.Services;
using Template_OrgName.Xncf.Template_XncfName.OHS.Local.PL;
using System;
using System.IO;
using System.Linq;


namespace Template_OrgName.Xncf.Template_XncfName.OHS.Local.AppService
{
    public class ChangeColorAppService: AppServiceBase
    {
        private ColorService _colorService;
        public ChangeColorAppService(IServiceProvider serviceProvider, ColorService colorService) : base(serviceProvider)
        {
            _colorService = colorService;
        }

        [FunctionRender("导出当前数据库 SQL 脚本", "导出当前站点正在使用的所有表的 SQL 脚本", typeof(Register))]
        public async Task<StringAppResponse> Brighten()
        {
            return await await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                _colorService.Brighten();
            });
        }
    }
}
