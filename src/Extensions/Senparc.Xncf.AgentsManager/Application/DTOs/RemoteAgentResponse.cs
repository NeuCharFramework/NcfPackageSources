/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RemoteAgentResponse.cs
    文件功能描述：增强 A2A 智能体、ChatGroup 执行能力与管理界面


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.AgentsManager.OHS.Local.PL
{
    public class RemoteAgent_GetListResponse
    {
        public PagedList<RemoteAgentDto> List { get; set; }
    }

    public class RemoteAgent_GetItemResponse
    {
        public RemoteAgentDto RemoteAgentDto { get; set; }
    }

    public class RemoteAgent_SetResponse
    {
        public RemoteAgentDto RemoteAgentDto { get; set; }
    }

    /// <summary>
    /// 批量执行连接检测的请求。空列表表示检测全部已配置的远程 Agent。
    /// </summary>
    public class RemoteAgent_TestConnectionsRequest
    {
        public List<int> RemoteAgentIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// 单个远程 Agent 的连接检测结果。即使检测失败也会作为正常结果返回，
    /// 使调用方能够一次看到整批失败项。
    /// </summary>
    public class RemoteAgent_ConnectionTestResult
    {
        public int RemoteAgentId { get; set; }
        public string Name { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public RemoteAgentDto RemoteAgentDto { get; set; }
    }

    public class RemoteAgent_TestConnectionsResponse
    {
        public List<RemoteAgent_ConnectionTestResult> Results { get; set; } = new List<RemoteAgent_ConnectionTestResult>();
        public int SuccessCount => Results.Count(z => z.Success);
        public int FailureCount => Results.Count(z => !z.Success);
    }
}
