/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MultipleDatabaseType.cs
    File description: MultipleDatabaseType related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum MultipleDatabaseType
    {
        Sqlite,
        SqlServer,
        MySql,
        InMemory,
        AzureCosmos,
        Oracle,
        PostgreSQL,
        Dm,
        Other = 99999,
        //UnitTest = 100000,
        //Legacy comment translated to English.
    }
}
