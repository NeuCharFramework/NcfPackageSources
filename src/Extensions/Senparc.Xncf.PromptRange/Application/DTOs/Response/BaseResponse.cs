/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BaseResponse.cs
    文件功能描述：BaseResponse 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class BaseResponse
    {
        public int Id { get; set; }

        // public DateTime CreateTime { get; set; }

        public DateTime LastRunTime { get; set; } = DateTime.Now;
    }
}