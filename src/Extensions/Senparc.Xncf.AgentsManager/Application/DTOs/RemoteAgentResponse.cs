/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteAgentResponse.cs
    File description: Related implementation.

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.AgentsManager.Application.Dtos
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
    /// Carries the remote-agent identifiers selected for connection testing.
    /// </summary>
    public class RemoteAgent_TestConnectionsRequest
    {
        public List<int> RemoteAgentIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Describes one remote-agent connection test result.
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
