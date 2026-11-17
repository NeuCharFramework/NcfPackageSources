/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: GenerateAppServiceInterface.cs
    File description: GenerateAppServiceInterface related implementation

    Created by: Senparc - 20220205

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{

    public class GenerateAppServiceInterface : AppServiceBase
    {
        public GenerateAppServiceInterface(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// generate generate AppService interfacecode
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.GenerateInterface.Name", "Function.XncfBuilder.GenerateInterface.Description", typeof(Register))]
        public Task<StringAppResponse> Generate(GenerateAppServiceInterface_GenerateRequest request)
        {
            return this.GetStringResponseAsync((response, logger) => Task.FromResult(""));
        }
    }
}
