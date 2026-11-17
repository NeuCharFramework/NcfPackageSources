/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfInterfaceConfig.cs
    File description: IXncfInterfaceConfig related implementation

    Created by: Senparc - 20241121

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.WebApi
{
    public interface IXncfInterfaceConfig
    {
        static string XncfName { get; }
        static string XncfAspireProjectName { get; }
    }
}
