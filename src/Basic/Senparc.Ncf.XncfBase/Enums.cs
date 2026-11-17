/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Enums.cs

    File description: Enums related implementation

    

    

    Created by: Senparc - 20231105

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// versionupdatetype
    /// </summary>
    public enum UpdateVersionType
    {
        NoUpdate = 0,
        MajorUpdate = 1,
        MinorUpdate = 2,
        PatchUpdate = 3,
    }
}
