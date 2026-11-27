/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleRepository.cs
    File description: XncfModuleRepository related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview12 Updated repository infrastructure and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.20.13.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Repository
{
    public class XncfModuleRepository : ClientRepositoryBase<XncfModule>
    {
        /// <summary>
        /// Initializes the repository for <see cref="XncfModule"/> records.
        /// </summary>
        /// <param name="db">The NCF database abstraction used for persistence.</param>
        public XncfModuleRepository(INcfDbData db) : base(db)
        {
        }
    }
}
