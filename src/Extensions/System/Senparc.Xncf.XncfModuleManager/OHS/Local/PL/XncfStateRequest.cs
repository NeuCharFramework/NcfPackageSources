using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using Senparc.Xncf.XncfModuleManager.Domain.Services;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.XncfModuleManager.OHS.Local.PL
{
    public class XncfState_ShowFunctionsRequest : FunctionAppRequestBase
    {
        [Description("XNCF 模块||查看具体 XNCF 模块的 Function 情况")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(XncfModuleOptions))]
        public string XncfModule { get; set; }

        [JsonIgnore]
        public SelectionList XncfModuleOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        public override Task LoadData(IServiceProvider serviceProvider)
        {
            var registers = Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList
                                     .OrderByDescending(z => (z.GetType().GetCustomAttributes(typeof(XncfOrderAttribute), true).FirstOrDefault() as XncfOrderAttribute)?.Order)
                                     .ToList();
            foreach (var item in registers)
            {
                XncfModuleOptions.Items.Add(new SelectionItem(item.Uid, item.Name));
            }

            return base.LoadData(serviceProvider);
        }
    }

    public class XncfState_InstallAndOpenModuleRequest : FunctionAppRequestBase
    {
        [Description("XNCF 模块||选择需要安装并开放的模块")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(XncfModuleOptions))]
        public string XncfModule { get; set; }

        [JsonIgnore]
        public SelectionList XncfModuleOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        public override async Task LoadData(IServiceProvider serviceProvider)
        {
            var xncfModuleServiceEx = serviceProvider.GetService<XncfModuleServiceExtension>();
            if (xncfModuleServiceEx != null)
            {
                var installedXncfModules = await xncfModuleServiceEx.GetFullListAsync(z => true).ConfigureAwait(false);
                var canInstallRegisters = xncfModuleServiceEx.GetUnInstallXncfModule(installedXncfModules)
                    .OrderBy(z => z.MenuName)
                    .ToList();

                foreach (var register in canInstallRegisters)
                {
                    XncfModuleOptions.Items.Add(new SelectionItem(register.Uid, $"{register.MenuName} ({register.Name})"));
                }
            }

            if (XncfModuleOptions.Items.Count == 0)
            {
                foreach (var register in XncfRegisterManager.RegisterList.Where(z => !z.IgnoreInstall).OrderBy(z => z.MenuName))
                {
                    XncfModuleOptions.Items.Add(new SelectionItem(register.Uid, $"{register.MenuName} ({register.Name})"));
                }
            }

            return;
        }
    }
}
