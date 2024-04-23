using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;

namespace Senparc.Xncf.XncfModuleManager.OHS.Local.PL
{
    public class XncfState_ShowFunctionsRequest : FunctionAppRequestBase
    {
        [Description("XNCF 模块||查看具体 XNCF 模块的 Function 情况")]
        public SelectionList XncfModule { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

        public override Task LoadData(IServiceProvider serviceProvider)
        {
            var registers = Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList
                                     .OrderByDescending(z => (z.GetType().GetCustomAttributes(typeof(XncfOrderAttribute), true).FirstOrDefault() as XncfOrderAttribute)?.Order)
                                     .ToList();
            foreach (var item in registers)
            {
                XncfModule.Items.Add(new SelectionItem(item.Uid, item.Name));
            }

            return base.LoadData(serviceProvider);
        }
    }
}
