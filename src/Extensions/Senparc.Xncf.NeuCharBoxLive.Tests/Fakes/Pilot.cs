/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Pilot.cs
    文件功能描述：测试替身：带有试点时期各项上限的 LiveOptions（默认配置已不设这些上限）


    创建标识：Senparc - 20261111

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 新增 Pilot：供测试上限行为的用例使用

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

/// <summary>
/// The bounds the pilot ran with (60 model calls and 30 minutes of work per request, a 10 minute approval wait, a 45
/// minute idle timeout). <see cref="LiveOptions"/> has none of them by default any more, so the tests of what happens at a
/// bound start from these.
/// </summary>
public static class Pilot
{
    public static LiveOptions Options(Action<LiveOptions>? change = null)
    {
        var o = new LiveOptions
        {
            MaxCalls = 60, MaxCallsWhatIf = 60, MaxWork = TimeSpan.FromMinutes(30),
            ApprovalWait = TimeSpan.FromMinutes(10), SessionIdle = TimeSpan.FromMinutes(45),
        };
        change?.Invoke(o);
        return o;
    }

    /// <summary>The same bounds as an <c>NcbLive</c> configuration section.</summary>
    public static Dictionary<string, string?> Config() => new()
    {
        ["NcbLive:MaxCalls"] = "60", ["NcbLive:MaxCallsWhatIf"] = "60", ["NcbLive:MaxWork"] = "00:30:00",
        ["NcbLive:ApprovalWait"] = "00:10:00", ["NcbLive:SessionIdle"] = "00:45:00",
    };
}
