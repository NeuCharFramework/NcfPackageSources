/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FirmwareUpdateFunctionRequests.cs
    文件功能描述：FirmwareUpdateFunctionRequests 相关实现
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    [Description("启用自动镜像||从 GitHub 同步 NCF 安装包到 ~/wwwroot/NcfPackages")]
    public bool AutoMirrorEnabled { get; set; }

    [Required]
    [Description("检查周期（小时）||1 至 24 小时")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(UpdateIntervalHoursOptions))]
    public int UpdateIntervalHours { get; set; } = 24;

    [JsonIgnore]
    public SelectionList UpdateIntervalHoursOptions { get; set; } = new(
        SelectionType.DropDownList,
        Enumerable.Range(1, 24).Select(h => new SelectionItem(h.ToString(), $"{h} 小时", $"每 {h} 小时检查一次", h == 24)).ToArray());

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
