/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfInterfaceConfig.cs
    File description: IXncfInterfaceConfig related implementation

    Created by: Senparc - 20241121

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
