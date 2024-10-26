using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupMemberService : ServiceBase<ChatGroupMember>
    {
        public ChatGroupMemberService(IRepositoryBase<ChatGroupMember> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}
