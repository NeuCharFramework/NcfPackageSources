/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RecallTestResponse.cs
    文件功能描述：RecallTestResponse 响应模型定义
    
    
    创建标识：Senparc - 20260225
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

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
        public double? Score { get; set; }
        public string Content { get; set; }
        public string RecallTime { get; set; }
    }
}
