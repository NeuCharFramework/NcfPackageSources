/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptShotting.cs
    File description: PromptRange shotting contracts for external modules

    Created by: Senparc - 20261122

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.Shotting;

public sealed record PromptShottingRange(int Id, string Name, int PromptCount);

public sealed class PromptShottingRequest
{
    public int? RangeId { get; set; }
    public string NewRangeName { get; set; }
    public string Prompt { get; set; }
    public int ModelId { get; set; }
    public bool Run { get; set; }
}

public sealed record PromptShottingResult(
    int RangeId,
    string RangeName,
    int PromptItemId,
    string PromptCode,
    string Output);

public interface IPromptShottingService
{
    Task<IReadOnlyList<PromptShottingRange>> GetRangesAsync();
    Task<PromptShottingResult> SaveAsync(PromptShottingRequest request);
}
