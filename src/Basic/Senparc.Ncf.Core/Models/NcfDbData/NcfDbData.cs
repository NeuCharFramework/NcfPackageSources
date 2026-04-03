using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// INcfDbData
    /// </summary>
    public interface INcfDbData
    {
        /// <summary>
        /// Force manual change of DetectChange
        /// </summary>
        bool ManualDetectChangeObject { get; set; }
        DbContext BaseDataContext { get; }
        void CloseConnection();
    }

    /// <summary>
    /// NcfDbData, NCF database context encapsulation, base class
    /// </summary>
    public abstract class NcfDbData : INcfDbData
    {
        /// <summary>
        /// Force manual change of DetectChange
        /// </summary>
        public virtual bool ManualDetectChangeObject { get; set; }
        public abstract DbContext BaseDataContext { get; }

        public abstract void CloseConnection();
    }
}
