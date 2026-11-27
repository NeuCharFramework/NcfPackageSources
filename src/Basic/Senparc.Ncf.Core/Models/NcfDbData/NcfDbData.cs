/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDbData.cs
    File description: NcfDbData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// INcfDbData
    /// </summary>
    public interface INcfDbData
    {
        /// <summary>
        /// Gets or sets whether change detection is controlled manually by the caller.
        /// </summary>
        bool ManualDetectChangeObject { get; set; }
        /// Gets the underlying EF Core context.
        DbContext BaseDataContext { get; }
        /// Closes the underlying database connection.
        void CloseConnection();
    }

    /// <summary>
    /// Base implementation of the database-data access wrapper.
    /// </summary>
    public abstract class NcfDbData : INcfDbData
    {
        /// <summary>
        /// Gets or sets whether change detection is controlled manually.
        /// </summary>
        public virtual bool ManualDetectChangeObject { get; set; }
        /// Gets the underlying EF Core context.
        public abstract DbContext BaseDataContext { get; }

        /// Closes the underlying database connection.
        public abstract void CloseConnection();
    }
}
