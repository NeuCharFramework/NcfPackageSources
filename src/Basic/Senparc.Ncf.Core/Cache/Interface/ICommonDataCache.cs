/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ICommonDataCache.cs

    File description: ICommonDataCache related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Cache
{
    public interface ICommonDataCache<T> : IBaseCache<T> where T : class, new()
    {
    }
}