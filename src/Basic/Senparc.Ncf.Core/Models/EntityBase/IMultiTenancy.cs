/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IMultiTenancy.cs
    File description: IMultiTenancy related implementation

    Created by: Senparc - 20201223

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// multi-tenancyinterface
    /// </summary>
    public interface IMultiTenancy
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TenantId { get; set; }
    }
}
