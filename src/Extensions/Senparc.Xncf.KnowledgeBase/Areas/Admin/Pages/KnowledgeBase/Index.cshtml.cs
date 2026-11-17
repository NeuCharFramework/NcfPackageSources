/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation
    
    
    Created by: Senparc - 20250105
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260815
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

----------------------------------------------------------------*/

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
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;
using Senparc.Xncf.KnowledgeBase.Domain.Services;

namespace Senparc.Xncf.KnowledgeBase.Areas.Admin.Pages.KnowledgeBase
{
    public class IndexModel : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly KnowledgeBaseService _knowledgeBaseService;
        private readonly IServiceProvider _serviceProvider;
        public KnowledgeBaseDto knowledgeBaseDto { get; set; }
        public string Token { get; set; }
        public string UpFileUrl { get; set; }
        public string BaseUrl { get; set; }

        public IndexModel(Lazy<XncfModuleService> xncfModuleService, KnowledgeBaseService knowledgeBaseService, IServiceProvider serviceProvider) : base(xncfModuleService)
        {
            CurrentMenu = "KnowledgeBase";
            this._knowledgeBaseService = knowledgeBaseService;
            this._serviceProvider = serviceProvider;
        }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;
        public PagedList<Models.DatabaseModel.KnowledgeBase> KnowledgeBase { get; set; }

        public Task OnGetAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnGetKnowledgeBasesAsync(string keyword, string orderField, int pageIndex, int pageSize, int? knowledgeBaseId)
        {
            keyword = keyword?.Trim();
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var seh = new SenparcExpressionHelper<Models.DatabaseModel.KnowledgeBase>();
            seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(keyword), _ => _.Name.Contains(keyword));
            seh.ValueCompare.AndAlso(knowledgeBaseId.HasValue && knowledgeBaseId.Value > 0,
                _ => _.Id == knowledgeBaseId.Value);
            var where = seh.BuildWhereExpression();
            var response = await _knowledgeBaseService.GetObjectListAsync(pageIndex, pageSize, where, orderField);
            return Ok(new
                    {
                        response.TotalCount,
                        response.PageIndex,
                        List = response.Select(_ => new {
                            _.Id,
                            _.EmbeddingModelId,
                            _.VectorDBId,
                            _.ChatModelId,
                            _.Name,
                            _.Content,
                            _.EmbeddedTime,
                            _.VectorCollectionName,
                            RetrievalConfig = KnowledgeBaseRetrievalConfig.FromJson(_.RetrievalConfigJson),
                            _.AddTime
                        })
                    });
        }
    }
}
