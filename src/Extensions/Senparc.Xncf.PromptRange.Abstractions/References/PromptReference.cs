/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptReference.cs
    File description: Related implementation.

    Created by: Senparc - 20261104

    Modified by: Senparc - 20261105
    Change description: v0.3.2-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

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

    /// <summary>
    /// Validates that prompt references are non-null, ordered, non-overlapping,
    /// positively sized, and contained within the supplied content.
    /// </summary>
    /// <param name="content">The source prompt content whose character range is referenced.</param>
    /// <param name="references">The ordered prompt references to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when more than 32 references are supplied or a reference is invalid,
    /// overlaps a preceding reference, or extends beyond the content length.
    /// </exception>
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
