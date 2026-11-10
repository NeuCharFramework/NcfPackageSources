/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PromptReferenceScoreRequest.cs
    文件功能描述：Prompt 引用评分请求参数


    创建标识：Senparc - 20261104

    修改标识：Senparc - 20261105
    修改描述：v0.20.14 增加 Prompt 引用评分请求

----------------------------------------------------------------*/
namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public sealed class PromptReferenceScoreRequest
{
    public int Id { get; set; }
    public decimal Score { get; set; }
}
