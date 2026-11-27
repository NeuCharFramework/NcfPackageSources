/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FirmwareUpdateConfig.cs
    File description: FirmwareUpdateConfig related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.11.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.FirmwareUpdate;

/// <summary>
/// Stores the persisted scheduling state for firmware mirror synchronization.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(FirmwareUpdateConfig))]
[Serializable]
public class FirmwareUpdateConfig : EntityBase<int>
{
    /// <summary>
    /// Gets or sets whether periodic mirror synchronization is enabled.
    /// </summary>
    public bool AutoMirrorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the interval between periodic synchronization attempts, in hours.
    /// </summary>
    public int UpdateIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the UTC timestamp of the last completed periodic synchronization.
    /// </summary>
    public DateTime? LastPeriodicSyncUtc { get; set; }
}
