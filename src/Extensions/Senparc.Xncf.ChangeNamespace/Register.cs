using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.ChangeNamespace
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.ChangeNamespace";
        public override string Uid => "476A8F12-860D-4B18-B703-393BBDEFBD85";//Must ensure global uniqueness and must be fixed after generation
        public override string Version => "0.3.9";//Version number is required

        public override string MenuName => "Modify namespace";
        public override string Icon => "fa fa-space-shuttle";//Reference such as: https://colorlib.com/polygon/gentelella/icons.html
        public override string Description => "This module allows developers to globally modify the namespace before installing NCF and releasing the product. Please use it with caution in a production environment. This operation is irreversible! You must make a backup in advance! It is not recommended to use this feature after it has been deployed to a production environment and is running!";

        ///// <summary>
        ///// Register the function modules that the current module needs to support
        ///// </summary>
        //public override IList<Type> Functions => new[] { 
        //    typeof(Functions.ChangeNamespace),
        //    typeof(Functions.RestoreNameSpace),
        //    typeof(Functions.DownloadSourceCode),
        //};

        public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion
    }
}
