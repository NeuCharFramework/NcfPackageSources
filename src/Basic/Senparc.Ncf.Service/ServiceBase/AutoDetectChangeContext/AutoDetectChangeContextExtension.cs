/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoDetectChangeContextExtension.cs
    File description: AutoDetectChangeContextExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;

namespace Senparc.Ncf.Service
{
    public static class AutoDetectChangeContextExtension
    {
       /// <summary>
        ///Legacy comment translated to English.
       /// </summary>
       /// <param name="serviceData"></param>
       /// <returns></returns>
       public static AutoDetectChangeContextWrap InstanceAutoDetectChangeContextWrap(this IServiceDataBase serviceData)
       {
           return new AutoDetectChangeContextWrap(serviceData);
       }

       /// <summary>
       ///Legacy comment translated to English.
       /// </summary>
       ///Legacy comment translated to English.
       /// <returns></returns>
       public static CloseAutoDetectChangeContext InstanceCloseAutoDetectChangeContext(this AutoDetectChangeContextWrap wrap)
       {
           return new CloseAutoDetectChangeContext(wrap);
       }

       /// <summary>
       ///Legacy comment translated to English.
       /// </summary>
       ///Legacy comment translated to English.
       /// <returns></wrap>
       public static void ForceDetectChange(this AutoDetectChangeContextWrap wrap, object entity)
       {
           wrap.ServiceData.BaseData.BaseDB.BaseDataContext.Entry(entity).State = EntityState.Modified;
       }
    }
}
