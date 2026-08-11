using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupRemoteMemberService : ServiceBase<ChatGroupRemoteMember>
    {
        public ChatGroupRemoteMemberService(IRepositoryBase<ChatGroupRemoteMember> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }
    }
}
