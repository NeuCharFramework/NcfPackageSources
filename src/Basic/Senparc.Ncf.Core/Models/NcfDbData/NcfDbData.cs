/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfDbData.cs

    File description: NcfDbData related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
        ///Legacy comment translated to English.
        /// </summary>
        bool ManualDetectChangeObject { get; set; }
        DbContext BaseDataContext { get; }
        void CloseConnection();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public abstract class NcfDbData : INcfDbData
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual bool ManualDetectChangeObject { get; set; }
        public abstract DbContext BaseDataContext { get; }

        public abstract void CloseConnection();
    }
}
