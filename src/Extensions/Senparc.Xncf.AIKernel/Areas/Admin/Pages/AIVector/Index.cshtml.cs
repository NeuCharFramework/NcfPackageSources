using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Areas.AIVector.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly AIVectorService aIVector;

        public Index(Lazy<XncfModuleService> xncfModuleService,AIVectorService aIVector) : base(xncfModuleService)
        {
            this.aIVector = aIVector;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetCategoryAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<Models.AIVector>();
            //seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Name.Contains(keyword));
            var where = seh.BuildWhereExpression();
            var response = await aIVector.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
            {
                response.TotalCount,
                response.PageIndex,
                List = response.Select(_ => new
                {
                    _.Id,
                    _.LastUpdateTime,
                    _.Remark,
                    _.Name,
                    _.AddTime
                })
            });
        }
    }
}
