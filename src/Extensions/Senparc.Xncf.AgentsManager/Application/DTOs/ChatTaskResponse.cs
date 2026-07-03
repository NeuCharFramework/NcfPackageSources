/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskResponse.cs
    文件功能描述：ChatTaskResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class ChatTask_GetListResponse
    {
        public PagedList<ChatTaskDto> ChatTaskList { get; set; }
    }

    public class ChatTask_GetItemResponse
    {
        public ChatTaskDto ChatTaskDto { get; set; }
    }
}
