using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;

namespace Senparc.IntegrationSample.Pages
{
    public class XncfRegisterItem
    {
        public IXncfRegister XncfRegister { get; set; }
        public List<RegisterFunctionInfo> RegisterFunctionInfoList { get; set; }


        public class RegisterFunctionInfo
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public FunctionRenderBag FunctionRenderBag { get; set; }
            public List<FunctionParameterInfo> FunctionParameterInfoList { get; set; }
        }
    }

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IServiceProvider _serviceProvider;

        public IList<XncfRegisterItem> XncfRegisterList { get; set; }


        public IndexModel(ILogger<IndexModel> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            this._serviceProvider = serviceProvider;
        }

        public async Task OnGetAsync()
        {
            XncfRegisterList = new List<XncfRegisterItem>();

            //所有Register
            var xncfRegisterList = Senparc.Ncf.XncfBase.XncfRegisterManager.RegisterList
                                     .OrderByDescending(z => (z.GetType().GetCustomAttributes(typeof(XncfOrderAttribute), true).FirstOrDefault() as XncfOrderAttribute)?.Order)
                                     .ToList();

            foreach (var item in xncfRegisterList)
            {
                XncfRegisterItem registerItem = new();
                registerItem.XncfRegister = item;
                registerItem.RegisterFunctionInfoList = new List<XncfRegisterItem.RegisterFunctionInfo>();

                XncfRegisterList.Add(registerItem);

                if (Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(item.GetType(), out var functionGroup))
                {
                    if (functionGroup == null)
                    {
                        continue;
                    }
                    //var funClass = _serviceProvider.GetService(funtionBag.MethodInfo.DeclaringType) as IAppService;
                    //var funMethod = funClass.GetType().GetMethod()

                    //遍历某个 Register 下所有的方法      TODO：未来可添加分组
                    foreach (var funtionBag in functionGroup.Values)
                    {
                        var result = await FunctionHelper.GetFunctionParameterInfoAsync(this._serviceProvider, funtionBag, true);

                        registerItem.RegisterFunctionInfoList.Add(new()
                        {
                            Key = funtionBag.Key,
                            Name = funtionBag.FunctionRenderAttribute.Name,
                            Description = funtionBag.FunctionRenderAttribute.Description,
                            FunctionRenderBag = funtionBag,
                            FunctionParameterInfoList = result
                        });
                    }
                }
            }
        }

    }
}
