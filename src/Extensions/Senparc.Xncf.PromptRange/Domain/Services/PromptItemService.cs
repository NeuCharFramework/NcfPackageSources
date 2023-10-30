using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptItemService : ServiceBase<PromptItem>
    {
        public PromptItemService(IRepositoryBase<PromptItem> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<PromptItem> AddPromptItemAsync(PromptItem_AddRequest request)
        {
            string name = request.Content.Length < 5 ? request.Content : request.Content.Substring(0, 5);

            PromptItem promptItem = new PromptItem(
               request.PresencePenalty, name,
               request.Content, request.ModelId, request.PromptGroupId, request.MaxToken,
               request.Temperature, request.TopP,
               request.FrequencyPenalty, 0, "",
               "", "", 0, "", DateTime.Now);

            promptItem.UpdateVersion();

            await base.SaveObjectAsync(promptItem);

            return promptItem;
        }


    }
}
