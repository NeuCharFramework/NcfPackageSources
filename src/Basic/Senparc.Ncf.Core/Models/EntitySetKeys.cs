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
    /// A collection of SetKeys of Entitey in all EntityFrameworks
    /// </summary>
    public static class EntitySetKeys
    {
        private static EntitySetKeysDictionary AllKeys = new EntitySetKeysDictionary();

        internal static ConcurrentBag<Type> DbContextStore { get; set; } = new ConcurrentBag<Type>();

        internal static object DbContextStoreLock = new object();

        /// <summary>
        /// Load the SetKey in the specified DbContext
        /// </summary>
        /// <param name="tryLoadDbContextType"></param>
        /// <param name="forceLoad">Try to force load, if it has been loaded before, delete and reload</param>
        /// <returns></returns>
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
                        return AllKeys;//It has already been loaded. Return the existing object directly and no longer load it.
                    }
                }
                else
                {
                    //force load
                    removeSuccess = EntitySetKeys.DbContextStore.TryTake(out tryLoadDbContextType);
                    SenparcTrace.BaseExceptionLog(new BaseException($"EntitySetKeys.DbContextStore.TryTake 失败，tryLoadDbContextType 类型：{tryLoadDbContextType.FullName}"));
                }

                if (removeSuccess)
                {
                    EntitySetKeys.DbContextStore.Add(tryLoadDbContextType);
                }

                //During initialization, the entity set name and entity category name are automatically read from the ORM.
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
                                AllKeys[dbSetType] = new SetKeyInfo(prop.Name, dbSetType, tryLoadDbContextType);//Get the first generic
                            }
                            else if (!AllKeys[dbSetType].SenparcEntityTypes.Contains(tryLoadDbContextType))
                            {
                                AllKeys[dbSetType].SenparcEntityTypes.Add(tryLoadDbContextType);//Add a new DbContext associated type to this dbSetType
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
        /// Get the Entity SetKey collection
        /// </summary>
        /// <param name="dbContextType">DbContext type</param>
        /// <returns></returns>
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
        /// Get the SetKey of all Entities
        /// </summary>
        /// <returns></returns>
        public static EntitySetKeysDictionary GetAllEntitySetInfo()
        {
            return AllKeys;
        }
    }


    public class SetKeyInfo
    {
        /// <summary>
        ///SetKey property name
        /// </summary>
        public string SetName { get; set; }
        /// <summary>
        ///DbSet property type
        /// </summary>
        public Type DbSetType { get; set; }
        /// <summary>
        ///SenparcEntity type
        /// </summary>
        public List<Type> SenparcEntityTypes { get; set; }

        public SetKeyInfo(string setName, Type dbSetType, Type senparcEntityType)
        {
            SetName = setName;
            DbSetType = dbSetType;
            SenparcEntityTypes = new List<Type>() { senparcEntityType };
        }
    }

    /// <summary>
    /// Entity set corresponding to ORM entity class
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