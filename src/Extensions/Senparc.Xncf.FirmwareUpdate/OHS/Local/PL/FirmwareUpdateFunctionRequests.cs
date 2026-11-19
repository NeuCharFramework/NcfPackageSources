/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FirmwareUpdateFunctionRequests.cs
    File description: FirmwareUpdateFunctionRequests related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Service;
using Senparc.Xncf.FirmwareUpdate;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;

namespace Senparc.Xncf.FirmwareUpdate.OHS.Local.PL;

public class FirmwareUpdate_ConfigureRequest : FunctionAppRequestBase
{
    [LocalizedDescription(typeof(FirmwareUpdateResource), "Parameter.FirmwareUpdate.AutoMirror")]
    public bool AutoMirrorEnabled { get; set; }

    [Required]
    [LocalizedDescription(typeof(FirmwareUpdateResource), "Parameter.FirmwareUpdate.Interval")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(UpdateIntervalHoursOptions))]
    public int UpdateIntervalHours { get; set; } = 24;

    [JsonIgnore]
    public SelectionList UpdateIntervalHoursOptions { get; set; } = new(
        SelectionType.DropDownList,
        Enumerable.Range(1, 24).Select(h => new SelectionItem(
            h.ToString(),
            FirmwareUpdateResource.Format("Parameter.FirmwareUpdate.Interval.Option", "{0} 小时", h),
            FirmwareUpdateResource.Format("Parameter.FirmwareUpdate.Interval.Help", "每 {0} 小时检查一次", h),
            h == 24)).ToArray());

    public override async Task LoadData(IServiceProvider serviceProvider)
    {
        var configService = serviceProvider.GetRequiredService<ServiceBase<FirmwareUpdateConfig>>();
        if (configService == null)
        {
            return;
        }

        var config = await configService.GetObjectAsync(_ => true).ConfigureAwait(false);
        if (config != null)
        {
            AutoMirrorEnabled = config.AutoMirrorEnabled;
            UpdateIntervalHours = Math.Clamp(config.UpdateIntervalHours, 1, 24);
        }
    }
}

public class FirmwareUpdate_SyncNowRequest : FunctionAppRequestBase
{
}
