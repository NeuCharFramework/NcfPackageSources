/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ChatTaskResponse.cs
    File description: ChatTaskResponse related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
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
