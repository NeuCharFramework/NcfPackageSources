using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// INcfDbData
    /// </summary>
    public interface INcfDbData
    {
        /// <summary>
        /// 强制手动更改DetectChange
        /// </summary>
        bool ManualDetectChangeObject { get; set; }
        DbContext BaseDataContext { get; }
        void CloseConnection();
    }

    /// <summary>
    /// NcfDbData，NCF 的数据库上下文封装，基础类
    /// </summary>
    public abstract class NcfDbData : INcfDbData
    {
        /// <summary>
        /// 强制手动更改DetectChange
        /// </summary>
        public virtual bool ManualDetectChangeObject { get; set; }
        public abstract DbContext BaseDataContext { get; }

        public abstract void CloseConnection();
    }
}
