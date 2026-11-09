/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdvanceSink.cs
    文件功能描述：访客推进时钟时把 clock、state、timeline 事件按 src=edit 推送出去


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>
/// The visitor's clock advance, streamed (spec 3.5): clock, state and timeline events with no rid, tagged src "edit".
/// Its state events are appended as the newest for their path, so an older edit's pending delayed frame can never land
/// after the advance's value. A clock jump never makes a device fail (faults only exist from a what-if), so
/// <see cref="FirstFaultAsync"/> just lets the clock carry on.
/// </summary>
public sealed class AdvanceSink(LiveSession session, RoomState? room = null) : IFastForwardSink
{
    /// <summary>A rule or schedule that fired, or a new day, changes the list: it streams with no rid, src "edit".</summary>
    public Task StepDoneAsync()
    {
        EditApplier.EmitAutomations(session, room ?? session.Room, null, edit: true);
        return Task.CompletedTask;
    }

    public Task ClockAsync(Clock c)
    {
        Append(Ev.ClockEv(c));
        return Task.CompletedTask;
    }

    public Task StateAsync(StateChange s)
    {
        EditApplier.EmitNewest(session, null, Ev.FromEdit(Ev.State(s)), s.Path);
        return Task.CompletedTask;
    }

    public Task RowAsync(TimelineRow r)
    {
        Append(Ev.Timeline(r));
        return Task.CompletedTask;
    }

    public Task<bool> FirstFaultAsync(string device, TimelineRow r) => Task.FromResult(true);

    /// <summary>Clock and timeline appends skip EmitGate: the stamps cover state paths only, and no delayed frame is a clock or a row.</summary>
    void Append((string kind, JsonObject data) e)
    {
        var edit = Ev.FromEdit(e);
        session.Events.Append(edit.kind, null, edit.data);
    }
}
