/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SceneManifest.cs
    文件功能描述：场景清单的数据结构：设备、动作、参数与安全状态


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

public enum ActionKind { Read, Write, Safe }

/// <summary>Parameters is a JSON Schema object. SafeWhen holds the arg values that make a Write a safe-state action.</summary>
public record ActionSpec(string Name, string Device, string Description, ActionKind Kind, JsonObject Parameters, JsonObject? SafeWhen)
{
    /// <summary>The catalog verb, for actions of a custom room's devices.</summary>
    public string? Verb { get; init; }
}

/// <summary>A field a visitor may edit by hand. Kind is "number" or "bool"; Min and Max (inclusive) apply to numbers.</summary>
public record EditSpec(string Field, string Kind, double? Min, double? Max);

public record DeviceSpec(string Id, string Name, string Ref, JsonObject Initial, IReadOnlyList<ActionSpec> Actions)
{
    /// <summary>The catalog type, for devices of a custom room.</summary>
    public string? Type { get; init; }
    public IReadOnlyList<EditSpec> Edits { get; init; } = [];
}

public record SceneManifest(string Id, string Title, DateTime StartDay, double StartHour, IReadOnlyList<string> Suggestions, IReadOnlyList<DeviceSpec> Devices)
{
    public ActionSpec? Action(string name) => Devices.SelectMany(d => d.Actions).FirstOrDefault(a => a.Name == name);
    public DeviceSpec? Device(string id) => Devices.FirstOrDefault(d => d.Id == id);
}
