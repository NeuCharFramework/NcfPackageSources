/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: PromptReferenceScoreRequest.cs
    File description: Prompt reference scoring request parameters


    Created by: Senparc - 20261104

    Modified by: Senparc - 20261105
    Change description: v0.20.14 Updated module behavior and related support.

----------------------------------------------------------------*/
namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

public sealed class PromptReferenceScoreRequest
{
    public int Id { get; set; }
    public decimal Score { get; set; }
}
