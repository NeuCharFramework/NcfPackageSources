using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Core.Models
{
    public interface INcfDbData
    {
        /// <summary>
        /// 强制手动更改DetectChange
        /// </summary>
        bool ManualDetectChangeObject { get; set; }
        DbContext BaseDataContext { get; }
        void CloseConnection();
    }

    public abstract class NcfDbData : INcfDbData
    {
        /// <summary>
        /// 强制手动更改DetectChange
        /// </summary>
        public bool ManualDetectChangeObject { get; set; }
        public abstract DbContext BaseDataContext { get; }

        public abstract void CloseConnection();
    }
}
