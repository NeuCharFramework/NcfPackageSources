/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PromptReference.cs
    文件功能描述：定义 PromptCode 引用范围、评分信息与共享评分契约


    创建标识：Senparc - 20261104

    修改标识：Senparc - 20261105
    修改描述：v0.3.2-preview6 提供引用范围校验及人工与 AI 评分契约

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.References;

public sealed class PromptReference
{
    public int PromptItemId { get; set; }
    public string PromptCode { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }

    public static void ValidateRanges(string content, IReadOnlyList<PromptReference> references)
    {
        if (references == null) return;
        if (references.Count > 32) throw new ArgumentException("At most 32 prompt references are allowed.");
        var end = 0;
        foreach (var reference in references)
        {
            if (reference == null || reference.PromptItemId <= 0 || string.IsNullOrWhiteSpace(reference.PromptCode) ||
                reference.Start < end || reference.Length <= 0 ||
                (long)reference.Start + reference.Length > (content?.Length ?? 0))
                throw new ArgumentException("Invalid or overlapping prompt reference.");
            end = reference.Start + reference.Length;
        }
    }
}

public sealed record PromptReferenceInfo(
    int PromptItemId, string PromptCode, decimal RobotScore, decimal HumanScore,
    decimal CurrentScore, bool HasBenchmark);

public interface IPromptReferenceService
{
    Task ValidateAsync(string content, IReadOnlyList<PromptReference> references);
    Task<PromptReferenceInfo> GetInfoAsync(int promptItemId);
    Task<PromptReferenceInfo> ScoreAsync(int promptItemId, decimal score);
    Task<PromptReferenceInfo> ScoreWithAIAsync(int promptItemId, string context, string result);
}
