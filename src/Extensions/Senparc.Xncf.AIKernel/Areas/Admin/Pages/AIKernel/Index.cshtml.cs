/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关实现
    
    
    创建标识：Senparc - 20231220
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Areas.AIKernel.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly AIModelService aIModel;

        public Index(Lazy<XncfModuleService> xncfModuleService,AIModelService aIModel) : base(xncfModuleService)
        {
            this.aIModel = aIModel;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetAIModelAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<Models.AIModel>();
            //seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Name.Contains(keyword));
            var where = seh.BuildWhereExpression();
            var response = await aIModel.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
            {
                response.TotalCount,
                response.PageIndex,
                List = response.Select(_ => new
                {
                    _.Id,
                    _.LastUpdateTime,
                    _.Remark,
                    Name = _.Alias,
                    _.DeploymentName,
                    _.AddTime
                })
            });
        }
    }
}
