/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ICommonDataCache.cs
    文件功能描述：ICommonDataCache 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Cache
{
    public interface ICommonDataCache<T> : IBaseCache<T> where T : class, new()
    {
    }
}