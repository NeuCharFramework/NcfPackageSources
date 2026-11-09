/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ModuleState.cs
    文件功能描述：模块运行时读取的 NCF 状态（模块安装与开放状态、AI Kernel 模型）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 IModuleState（XncfModuleService）与 IAiModelSource（AIModelService）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 IAiModelSource 新增 ListAsync，供设置页的模型选择使用

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>This module in NCF's module table.</summary>
public enum ModuleStatus { NotInstalled, Closed, Open }

/// <summary>Whether this module is installed, and open, in NCF.</summary>
public interface IModuleState
{
    Task<ModuleStatus> GetAsync(CancellationToken ct);
}

/// <summary>AI Kernel's models by Id.</summary>
public interface IAiModelSource
{
    /// <summary>The model with that Id, or null when there is none (never created, or deleted).</summary>
    Task<AIModelDto?> GetAsync(int id, CancellationToken ct);

    /// <summary>Every model, for the Settings page's pickers (the page maps them to <c>ModelChoice</c>, which has no key).</summary>
    Task<IReadOnlyList<AIModelDto>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AIModelDto>>([]);
}

/// <summary>
/// NCF's module table through <see cref="XncfModuleService"/> (a scoped service, so a scope per read): no row for
/// <paramref name="uid"/> (or a deleted one) is NotInstalled, State 开放 is Open, any other state (关闭, 新增待审核, 更新待审核) is Closed.
/// </summary>
public sealed class XncfModuleState(IServiceScopeFactory scopes, string uid) : IModuleState
{
    public async Task<ModuleStatus> GetAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var modules = scope.ServiceProvider.GetRequiredService<XncfModuleService>();
        var module = await modules.GetObjectAsync(z => z.Uid == uid).ConfigureAwait(false);
        return StatusOf(module?.State);
    }

    internal static ModuleStatus StatusOf(XncfModules_State? state) => state switch
    {
        null => ModuleStatus.NotInstalled,
        XncfModules_State.开放 => ModuleStatus.Open,
        _ => ModuleStatus.Closed,
    };
}

/// <summary>
/// AI Kernel's models through <see cref="AIModelService"/> (scoped, so a scope per read), mapped to <see cref="AIModelDto"/>
/// the way AI Kernel itself does (<c>new AIModelDto(model)</c>).
/// </summary>
public sealed class AiKernelModelSource(IServiceScopeFactory scopes) : IAiModelSource
{
    public async Task<AIModelDto?> GetAsync(int id, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var models = scope.ServiceProvider.GetRequiredService<AIModelService>();
        var model = await models.GetObjectAsync(z => z.Id == id).ConfigureAwait(false);
        return model is null ? null : new AIModelDto(model);
    }

    public async Task<IReadOnlyList<AIModelDto>> ListAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var models = scope.ServiceProvider.GetRequiredService<AIModelService>();
        var all = await models.GetFullListAsync(z => true).ConfigureAwait(false);
        return all.OrderBy(m => m.Id).Select(m => new AIModelDto(m)).ToList();
    }
}
