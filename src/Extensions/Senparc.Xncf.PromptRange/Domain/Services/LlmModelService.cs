using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading.Tasks;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class LlmModelService : ServiceBase<LlmModel>
    {
        public LlmModelService(IRepositoryBase<LlmModel> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public Task<LlmModel> GetLlmModelById(int Id)
        {
            return base.GetObjectAsync(n => n.Id == Id);
        }

        public LlmModel Add(LlmModel_AddRequest request)
        {
            #region validate
            // 如果是Azure OpenAI
            if (request.ModelType == Constants.ModelTypeEnum.AzureOpenAI.ToString()) 
            {
                // 强制要求ApiVersion和Endpoint不为空
                if (string.IsNullOrWhiteSpace(request.ApiVersion) || string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    throw new NcfExceptionBase("使用AzuerOpenAI时，ApiVersion和Endpoint不能为空");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.ApiVersion) && !Constants.ApiVersionList.Contains(request.ApiVersion))
            {
                // ApiVersion不为空且不在ApiVersionList中
                throw new NcfExceptionBase("ApiVersion不存在");
            }
            #endregion

            LlmModel model = new LlmModel(
                request.Name, request.Endpoint, request.ModelType,
                request.OrganizationId, request.ApiKey, request.ApiVersion, 
                "", 0, "", "", "");

            model.Switch(true);
            return model;
        }
    }
}
