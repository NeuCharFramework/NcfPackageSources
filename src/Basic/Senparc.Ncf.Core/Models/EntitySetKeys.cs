/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntitySetKeys.cs
    File description: EntitySetKeys related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET.Exceptions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Discovers and indexes DbSet-to-entity mappings for registered EF Core contexts.
    /// </summary>
    public static class EntitySetKeys
    {
        private static EntitySetKeysDictionary AllKeys = new EntitySetKeysDictionary();

        internal static ConcurrentBag<Type> DbContextStore { get; set; } = new ConcurrentBag<Type>();

        internal static object DbContextStoreLock = new object();

        /// <summary>
        /// Discovers entity sets exposed by a DbContext type.
        /// </summary>
        /// <param name="tryLoadDbContextType">The DbContext type to inspect.</param>
        /// <param name="forceLoad">Whether to rescan a context already discovered.</param>
        /// <returns>The accumulated entity-set mapping dictionary.</returns>
        /// <exception cref="ArgumentException">Thrown when the type is not a DbContext.</exception>
        public static EntitySetKeysDictionary TryLoadSetInfo(Type tryLoadDbContextType, bool forceLoad = false)
        {
            lock (EntitySetKeys.DbContextStoreLock)
            {
                if (!tryLoadDbContextType.IsSubclassOf(typeof(DbContext)))
                {
                    throw new ArgumentException($"{nameof(tryLoadDbContextType)}不是 DbContext 的子类！", nameof(tryLoadDbContextType));
                }

                var removeSuccess = true;
                if (!forceLoad)
                {
                    if (EntitySetKeys.DbContextStore.Contains(tryLoadDbContextType))
                    {
                        return AllKeys;//Legacy comment translated to English.
                    }
                }
                else
                {
                    //Legacy comment translated to English.
                    removeSuccess = EntitySetKeys.DbContextStore.TryTake(out tryLoadDbContextType);
                    SenparcTrace.BaseExceptionLog(new BaseException($"EntitySetKeys.DbContextStore.TryTake 失败，tryLoadDbContextType 类型：{tryLoadDbContextType.FullName}"));
                }

                if (removeSuccess)
                {
                    EntitySetKeys.DbContextStore.Add(tryLoadDbContextType);
                }

                //Legacy comment translated to English.
                var clientProperties = tryLoadDbContextType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);

                var properities = new List<PropertyInfo>();
                properities.AddRange(clientProperties);

                foreach (var prop in properities)
                {
                    try
                    {
                        //ObjectQuery，ObjectSet for EF4，DbSet for EF Code First
                        if (prop.PropertyType.Name.IndexOf("DbSet") != -1 && prop.PropertyType.GetGenericArguments().Length > 0)
                        {
                            var dbSetType = prop.PropertyType.GetGenericArguments()[0];
                            if (!AllKeys.ContainsKey(dbSetType))
                            {
                                AllKeys[dbSetType] = new SetKeyInfo(prop.Name, dbSetType, tryLoadDbContextType);//Legacy comment translated to English.
                            }
                            else if (!AllKeys[dbSetType].SenparcEntityTypes.Contains(tryLoadDbContextType))
                            {
                                AllKeys[dbSetType].SenparcEntityTypes.Add(tryLoadDbContextType);//Legacy comment translated to English.
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"{nameof(EntitySetKeys)}.TryLoadSetInfo() 发生异常：");
                        Console.WriteLine(ex);
                    }
                }
            }
            return AllKeys;
        }

        /// <summary>
        /// Returns mappings associated with a specific DbContext type.
        /// </summary>
        /// <param name="dbContextType">The DbContext type to inspect.</param>
        /// <returns>The entity-set mappings associated with the context.</returns>
        public static EntitySetKeysDictionary GetEntitySetInfo(Type dbContextType)
        {
            EntitySetKeysDictionary dic = new EntitySetKeysDictionary();
            foreach (var setKeyInfo in AllKeys.Values.Where(z => z.SenparcEntityTypes.Contains(dbContextType)))
            {
                if (!dic.ContainsKey(setKeyInfo.DbSetType))
                {
                    var addSuccess = dic.TryAdd(setKeyInfo.DbSetType, setKeyInfo);
                    if (!addSuccess)
                    {
                        SenparcTrace.BaseExceptionLog(new NcfDatabaseException($"GetEntitySetInfo 发生异常，DbSetType：{setKeyInfo.DbSetType.Name}，setKeyInfo：{setKeyInfo.ToJson()}", null, dbContextType));
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// Gets all mappings discovered across registered DbContext types.
        /// </summary>
        /// <returns>The shared entity-set mapping dictionary.</returns>
        public static EntitySetKeysDictionary GetAllEntitySetInfo()
        {
            return AllKeys;
        }
    }


    public class SetKeyInfo
    {
        /// <summary>
        /// Gets or sets the DbSet property name.
        /// </summary>
        public string SetName { get; set; }
        /// <summary>
        /// Gets or sets the entity type represented by the DbSet.
        /// </summary>
        public Type DbSetType { get; set; }
        /// <summary>
        /// Gets the DbContext types that expose this entity.
        /// </summary>
        public List<Type> SenparcEntityTypes { get; set; }

        /// <summary>
        /// Initializes an entity-set mapping record.
        /// </summary>
        /// <param name="setName">The DbSet property name.</param>
        /// <param name="dbSetType">The entity type represented by the DbSet.</param>
        /// <param name="senparcEntityType">The initial DbContext type exposing the entity.</param>
        public SetKeyInfo(string setName, Type dbSetType, Type senparcEntityType)
        {
            SetName = setName;
            DbSetType = dbSetType;
            SenparcEntityTypes = new List<Type>() { senparcEntityType };
        }
    }

    /// <summary>
    /// Provides typed lookup behavior for discovered entity-set mappings.
    /// </summary>
    public class EntitySetKeysDictionary : ConcurrentDictionary<Type, SetKeyInfo>
    {
        public new SetKeyInfo this[Type entityType]
        {
            get
            {
                if (!base.ContainsKey(entityType))
                {
                    throw new Exception($"未找到实体类型：{entityType.FullName}");
                }
                return base[entityType];
            }
            set
            {
                base[entityType] = value;
            }
        }
    }
}
