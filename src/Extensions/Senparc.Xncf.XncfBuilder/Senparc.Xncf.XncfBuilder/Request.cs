/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Request.cs
    文件功能描述：Request 相关实现
    
    
    创建标识：Senparc - 20250624
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder
{
    public class Request
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Body { get; set; }

        // 新增字段测试动态更新
        public Dictionary<string, string>? Headers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
