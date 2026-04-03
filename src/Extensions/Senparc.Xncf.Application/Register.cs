using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Application.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.Application
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IRegister 接口

        public override string Name => "Senparc.Xncf.Application";
        public override string Uid => "699DFE0D-C1C0-4315-87DF-0DE1502B87A9";//Must ensure global uniqueness and must be fixed after generation
        public override string Version => "0.0.5";//Version number is required

        public override string MenuName => "application module";
        public override string Icon => "fa fa-pencil";
        public override string Description => "This module provides developers with a way to launch any program!";

        /// <summary>
        /// Register the function modules that the current module needs to support
        /// </summary>
        public override IList<Type> Functions => new[] {
            typeof(Functions.LaunchApp),
        };

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
