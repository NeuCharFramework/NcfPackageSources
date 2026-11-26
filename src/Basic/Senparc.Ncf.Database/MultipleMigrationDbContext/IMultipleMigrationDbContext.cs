/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IMultipleMigrationDbContext.cs
    File description: IMultipleMigrationDbContext related implementation

    Created by: Senparc - 20201007

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database.MultipleMigrationDbContext
{
    /// <summary>
    /// Marks a DbContext as participating in provider-specific migration registration.
    /// </summary>
    public interface IMultipleMigrationDbContext
    {
        /////// <summary>
        /////// databasetype
        /////// </summary>
        //MultipleDatabaseType MultipleDatabaseType { get; }
    }
}
