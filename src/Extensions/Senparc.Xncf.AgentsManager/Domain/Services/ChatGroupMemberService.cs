/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupMemberService.cs
    文件功能描述：ChatGroupMemberService 相关实现
    
    
    创建标识：Senparc - 20241017
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
