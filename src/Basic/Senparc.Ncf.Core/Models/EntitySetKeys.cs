using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 所有 EntityFramework 中 Entitey 的 SetKey 的集合
    /// </summary>
    public static class EntitySetKeys
    {
        private static EntitySetKeysDictionary AllKeys = new EntitySetKeysDictionary();

        internal static ConcurrentBag<Type> DbContextStore { get; set; } = new ConcurrentBag<Type>();

        internal static object DbContextStoreLock = new object();

        /// <summary>
        /// 加载制定 DbContext 中的 SetKey
        /// </summary>
        /// <param name="tryLoadDbContextType"></param>
        /// <returns></returns>
        public static EntitySetKeysDictionary TryLoadSetInfo(Type tryLoadDbContextType)
        {
            lock (EntitySetKeys.DbContextStoreLock)
            {
                if (!tryLoadDbContextType.IsSubclassOf(typeof(DbContext)))
                {
                    throw new ArgumentException($"{nameof(tryLoadDbContextType)}不是 DbContext 的子类！", nameof(tryLoadDbContextType));
                }

                if (EntitySetKeys.DbContextStore.Contains(tryLoadDbContextType))
                {
                    return AllKeys;
                }

                EntitySetKeys.DbContextStore.Add(tryLoadDbContextType);

                //初始化的时候从ORM中自动读取实体集名称及实体类别名称
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
                            AllKeys[dbSetType] = new SetKeyInfo(prop.Name, dbSetType, tryLoadDbContextType);//获取第一个泛型
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return AllKeys;
        }

        /// <summary>
        /// 获取 Entity SetKey 集合
        /// </summary>
        /// <param name="dbContextType">DbContext 类型</param>
        /// <returns></returns>
        public static EntitySetKeysDictionary GetEntitySetInfo(Type dbContextType)
        {
            EntitySetKeysDictionary dic = new EntitySetKeysDictionary();
            foreach (var setKeyInfo in AllKeys.Values.Where(z => z.SenparcEntityType == dbContextType))
            {
                dic.TryAdd(setKeyInfo.DbSetType, setKeyInfo);
            }
            return dic;
        }

        /// <summary>
        /// 获取所有 Entity 的 SetKey
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
        /// SetKey 属性名称
        /// </summary>
        public string SetName { get; set; }
        /// <summary>
        /// DbSet 属性类型
        /// </summary>
        public Type DbSetType { get; set; }
        /// <summary>
        /// SenparcEntity 类型
        /// </summary>
        public Type SenparcEntityType { get; set; }

        public SetKeyInfo(string setName, Type dbSetType, Type senparcEntityType)
        {
            SetName = setName;
            DbSetType = dbSetType;
            SenparcEntityType = senparcEntityType;
        }
    }

    /// <summary>
    /// 与ORM实体类对应的实体集
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