/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptRangeSkillTestService.cs
    File description: Related implementation.

    Created by: Senparc - 20261021

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.PromptRange.Abstractions.Skills;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class PromptRangeSkillTestService : ISkillTestRunner
{
    private readonly ISkillCatalog _catalog;
    private readonly ISkillExecutor _executor;

    /// <summary>
    /// Initializes the PromptRange skill test runner.
    /// </summary>
    /// <param name="catalog">The skill catalog used to resolve descriptors.</param>
    /// <param name="executor">The PromptRange skill executor.</param>
    public PromptRangeSkillTestService(ISkillCatalog catalog, ISkillExecutor executor)
    {
        _catalog = catalog;
        _executor = executor;
    }

    /// <summary>
    /// Resolves and executes a PromptRange skill in evaluation mode.
    /// Workspace and Sandbox isolation requests are rejected because this runner
    /// intentionally provides PromptOnly execution only.
    /// </summary>
    /// <param name="request">The skill identifier, optional version, input, caller module, and isolation request.</param>
    /// <param name="cancellationToken">Token used to cancel catalog lookup or execution.</param>
    /// <returns>A test result containing output, success state, isolation state, and diagnostics.</returns>
    /// <exception cref="ArgumentException">Thrown when the request or SkillId is empty.</exception>
    public async Task<SkillTestResult> TestAsync(
        SkillTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SkillId))
        {
            throw new ArgumentException("SkillId 不能为空", nameof(request));
        }

        var diagnostics = new List<string>();
        var descriptor = await _catalog.GetAsync(
            request.SkillId,
            request.Version,
            new SkillExecutionContext("PromptRange", Mode: SkillExecutionMode.Preview),
            cancellationToken);

        diagnostics.Add($"Skill={descriptor.SkillId}");
        diagnostics.Add($"Version={descriptor.CurrentVersion}");
        diagnostics.Add($"Isolation={request.Isolation}");

        if (request.Isolation is SkillTestIsolation.Workspace or SkillTestIsolation.Sandbox)
        {
            return new SkillTestResult(
                request.SkillId,
                request.Version ?? descriptor.CurrentVersion,
                request.Isolation,
                false,
                false,
                Error: "当前 PromptRange 执行器只允许 PromptOnly。Workspace/Sandbox 测试必须由对应隔离适配器接管。",
                Diagnostics: diagnostics);
        }

        var result = await _executor.ExecuteAsync(
            new SkillExecutionRequest(
                request.SkillId,
                request.Version,
                request.Input,
                new SkillExecutionContext(
                    request.CallerModule,
                    Mode: SkillExecutionMode.Evaluation),
                PersistResult: false),
            cancellationToken);

        diagnostics.Add("No workspace or external tool was enabled.");
        return new SkillTestResult(
            request.SkillId,
            result.Version,
            SkillTestIsolation.PromptOnly,
            result.Succeeded,
            true,
            result.Output,
            result.Error,
            diagnostics);
    }
}
