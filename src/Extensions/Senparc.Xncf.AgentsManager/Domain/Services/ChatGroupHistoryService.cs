using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupHistoryService : ServiceBase<ChatGroupHistory>
    {
        public ChatGroupHistoryService(IRepositoryBase<ChatGroupHistory> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task CreateHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            var chatGroupHistory = new ChatGroupHistory(chatGroupHistoryDto);
            await base.SaveObjectAsync(chatGroupHistory);
        }
    }
}
