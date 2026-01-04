using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Service;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Utility;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBases
{
    public class IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly KnowledgeBaseService _knowledgeBasesService;
        private readonly IServiceProvider _serviceProvider;
        public KnowledgeBasesDto knowledgeBasesDto { get; set; }
        public string Token { get; set; }
        public string UpFileUrl { get; set; }
        public string BaseUrl { get; set; }

        public IndexModel(Lazy<XncfModuleService> xncfModuleService, KnowledgeBaseService knowledgeBasesService, IServiceProvider serviceProvider) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBases";
            this._knowledgeBasesService = knowledgeBasesService;
            this._serviceProvider = serviceProvider;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;
        public PagedList<Models.DatabaseModel.KnowledgeBase> KnowledgeBases { get; set; }

        public Task OnGetAsync()
        {
            BaseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            UpFileUrl = $"{BaseUrl}/api/v1/common/upload";
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnGetKnowledgeBasesAsync(string keyword, string orderField, int pageIndex, int pageSize)
        {
            var seh = new SenparcExpressionHelper<Models.DatabaseModel.KnowledgeBase>();
            //seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Name.Contains(keyword));
            var where = seh.BuildWhereExpression();
            var response = await _knowledgeBasesService.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
                    {
                        response.TotalCount,
                        response.PageIndex,
                        List = response.Select(_ => new {
                            _.Id,
                            _.LastUpdateTime,
                            _.Remark,
                            _.EmbeddingModelId,
                            _.VectorDBId,
                            _.ChatModelId,
                            _.Name,
                            _.Content,
                            _.AddTime
                        })
                    });
        }
    }
}
