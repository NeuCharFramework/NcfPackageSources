/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20231220

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes the AI model administration page.
        /// </summary>
        /// <param name="xncfModuleService">The lazy XNCF module service.</param>
        /// <param name="aIModel">The AI model service.</param>
        public Index(Lazy<XncfModuleService> xncfModuleService,AIModelService aIModel) : base(xncfModuleService)
        {
            this.aIModel = aIModel;
        }

        /// <summary>
        /// Initializes the AI model page request.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Returns a paged list of AI models for the administration grid.
        /// </summary>
        /// <param name="keyword">The optional search keyword.</param>
        /// <param name="orderField">The textual order expression.</param>
        /// <param name="pageIndex">The requested page index.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <returns>An HTTP 200 JSON page.</returns>
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
