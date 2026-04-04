using Senparc.Ncf.XncfBase.FunctionRenders;
using System.ComponentModel;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

/// <summary>
/// 查看 PromptCode 列表的请求（用于 FunctionRender）
/// </summary>
public class PromptRange_ViewPromptCodeRequest : FunctionAppRequestBase
{
    [Description("筛选靶场名称||输入靶场名称关键字进行过滤（可为空，表示显示所有靶场）")]
    public string FilterRangeName { get; set; }
}
