using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.FirmwareUpdate;

/// <summary>
/// NCF 安装包镜像任务配置（单行配置）
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(FirmwareUpdateConfig))]
[Serializable]
public class FirmwareUpdateConfig : EntityBase<int>
{
    /// <summary>
    /// 是否启用定时从 GitHub 同步安装包
    /// </summary>
    public bool AutoMirrorEnabled { get; set; }

    /// <summary>
    /// 检查/同步周期（小时），范围 1–24
    /// </summary>
    public int UpdateIntervalHours { get; set; } = 24;

    /// <summary>
    /// 上次按周期成功完成同步的 UTC 时间
    /// </summary>
    public DateTime? LastPeriodicSyncUtc { get; set; }
}
