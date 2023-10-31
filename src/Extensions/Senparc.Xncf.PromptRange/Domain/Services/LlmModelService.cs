using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading.Tasks;

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
            LlmModel model = new LlmModel(request.Name, request.Endpoint, request.OrganizationId, request.ApiKey, "", "", 0, "", "", "");

            model.Switch(true);
            return model;
        }
    }
}
