/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: FirmwareUpdateConfig.cs
    File description: FirmwareUpdateConfig related implementation
    
    
    Created by: Senparc - 20260504
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.FirmwareUpdate;

/// <summary>
///Legacy comment translated to English.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(FirmwareUpdateConfig))]
[Serializable]
public class FirmwareUpdateConfig : EntityBase<int>
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool AutoMirrorEnabled { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int UpdateIntervalHours { get; set; } = 24;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public DateTime? LastPeriodicSyncUtc { get; set; }
}
