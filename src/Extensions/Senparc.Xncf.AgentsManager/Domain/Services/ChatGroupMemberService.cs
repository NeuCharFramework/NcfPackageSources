/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ChatGroupMemberService.cs

    File description: ChatGroupMemberService related implementation

    

    

    Created by: Senparc - 20241017

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
