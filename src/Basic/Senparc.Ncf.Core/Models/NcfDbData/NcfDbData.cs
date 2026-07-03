/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfDbData.cs
    文件功能描述：NcfDbData 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
