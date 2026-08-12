/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RecallTestResponse.cs
    文件功能描述：RecallTestResponse 响应模型定义
    
    
    创建标识：Senparc - 20260225
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response
{
    public class RecallTestResponse
    {
        /// <summary>当前测试中的返回顺序，从 1 开始。</summary>
        public int Rank { get; set; }

        /// <summary>向量库返回的相似度分数，仅适合在同一检索配置下比较。</summary>
        public double? Score { get; set; }

        public string Content { get; set; }
        public int ContentLength { get; set; }
        public string RecallTime { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string SourceName { get; set; }
        public string SourceLink { get; set; }
    }
}
