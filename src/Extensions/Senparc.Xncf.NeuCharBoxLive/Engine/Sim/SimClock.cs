/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SimClock.cs
    文件功能描述：模拟时钟：把小时数换算成日期时间与标签


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

public record Clock(string Iso, string Label, double Hour);

public static class SimClock
{
    public static Clock At(DateTime startDay, double hour)
    {
        var t = startDay.Date.AddSeconds(Math.Round(hour * 3600));
        return new Clock(t.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
            t.ToString("ddd HH:mm", CultureInfo.InvariantCulture), hour);
    }
}
