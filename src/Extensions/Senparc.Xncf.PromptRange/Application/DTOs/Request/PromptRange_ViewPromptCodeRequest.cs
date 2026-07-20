/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptRange_ViewPromptCodeRequest.cs
    文件功能描述：PromptRange_ViewPromptCodeRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.17.0-preview5 为 PromptRange 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

/// <summary>
/// 查看 PromptCode 列表的请求（用于 FunctionRender）
/// </summary>
public class PromptRange_ViewPromptCodeRequest : FunctionAppRequestBase
{
    [LocalizedDescription(typeof(PromptRangeResource), "Parameter.PromptRange.FilterRange")]
    public string FilterRangeName { get; set; }
}
