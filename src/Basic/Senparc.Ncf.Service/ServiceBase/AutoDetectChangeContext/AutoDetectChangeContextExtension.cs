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
       /// Creates a disposable scope that coordinates automatic entity-change detection
       /// through the supplied service data object.
       /// </summary>
       /// <param name="serviceData">The service data object whose database context is controlled by the scope.</param>
       /// <returns>A wrapper that can be disposed to close the change-detection scope.</returns>
       public static AutoDetectChangeContextWrap InstanceAutoDetectChangeContextWrap(this IServiceDataBase serviceData)
       {
           return new AutoDetectChangeContextWrap(serviceData);
       }

       /// <summary>
       /// Creates a disposable closer for an existing automatic change-detection wrapper.
       /// </summary>
       /// <param name="wrap">The wrapper whose automatic detection scope should be closed.</param>
       /// <returns>A disposable closer associated with <paramref name="wrap"/>.</returns>
       public static CloseAutoDetectChangeContext InstanceCloseAutoDetectChangeContext(this AutoDetectChangeContextWrap wrap)
       {
           return new CloseAutoDetectChangeContext(wrap);
       }

       /// <summary>
       /// Forces Entity Framework Core to treat an entity as modified in the database context
       /// owned by the supplied change-detection wrapper.
       /// </summary>
       /// <param name="wrap">The wrapper that exposes the target service data and database context.</param>
       /// <param name="entity">The entity whose state should be set to <see cref="EntityState.Modified"/>.</param>
       public static void ForceDetectChange(this AutoDetectChangeContextWrap wrap, object entity)
       {
           wrap.ServiceData.BaseData.BaseDB.BaseDataContext.Entry(entity).State = EntityState.Modified;
       }
    }
}
