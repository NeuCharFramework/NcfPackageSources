/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FullEntityCache.cs
    File description: FullEntityCache related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Log;
using System;

namespace Senparc.Ncf.Core.Utility
{
    public static class FullEntityCache
    {
        /// <summary>
        /// Copies properties marked with <see cref="AutoSetCacheAttribute"/> from an entity to a cache projection.
        /// </summary>
        /// <typeparam name="TFullEntity">The cache projection type.</typeparam>
        /// <typeparam name="TEntity">The source entity type.</typeparam>
        /// <param name="fullEntity">The cache projection receiving values.</param>
        /// <param name="entity">The source entity.</param>
        /// <exception cref="Exception">Thrown when either object is null or a marked property is missing/incompatible.</exception>
        public static void SetFullEntityCache<TFullEntity, TEntity>(TFullEntity fullEntity, TEntity entity)
        {
            if (fullEntity == null || entity == null)
            {
                throw new Exception("参数不可以为NULL");
            }

            try
            {
                var fullEntityType = fullEntity.GetType();
                var entityType = entity.GetType();
                var props = fullEntityType.GetProperties();
                foreach (var p in props)
                {
                    //Legacy comment translated to English.
                    AutoSetCacheAttribute m = Attribute.GetCustomAttribute(p, typeof(AutoSetCacheAttribute)) as AutoSetCacheAttribute;
                    if (m != null)
                    {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        var entityProp = entityType.GetProperty(p.Name);
                        if (entityProp == null || entityProp.PropertyType != p.PropertyType)
                        {
                            throw new Exception("原始实体没有相同类型和名称的属性存在：" + p.Name);
                        }
                        p.SetValue(fullEntity, entityProp.GetValue(entity, null), null);
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtility.SystemLogger.Debug("跟踪Cache错误：" + ex.Message, ex);
                throw ex;
            }
        }
    }
}
