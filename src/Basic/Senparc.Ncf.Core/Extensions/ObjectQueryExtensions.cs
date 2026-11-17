/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ObjectQueryExtensions.cs
    File description: ObjectQueryExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Ncf.Core.Extensions
{
    public static class ObjectQueryExtensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        public static IQueryable<T> Includes<T>(this IQueryable<T> obj, string[] includes) where T : class
        {
            //Legacy comment translated to English.
            if (includes == null)
            {
                return obj;
            }

            foreach (var item in includes)
            {
                obj = obj.Include(item);
            }
            return obj;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="includeTyles"></param>
        /// <returns></returns>
        public static IQueryable<T> Includes<T>(this IQueryable<T> obj, Type[] includeTyles) where T : class
        {
            //Legacy comment translated to English.
            if (includeTyles == null)
                return obj;

            foreach (var item in includeTyles)
            {
                obj = obj.Include(item);
            }
            return obj;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="includeType"></param>
        /// <returns></returns>
        public static IQueryable<T> Include<T>(this IQueryable<T> obj, Type includeType) where T : class
        {
            //Legacy comment translated to English.
            return obj.Include(includeType.Name);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="orderBy"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T, TK>(this IQueryable<T> obj, Expression<Func<T, TK>> orderBy, OrderingType orderingType) where T : class
        {
            if (orderBy == null)
                throw new Exception("OrderBy can not be Null！");

            return orderingType == OrderingType.Ascending ? obj.OrderBy(orderBy) : obj.OrderByDescending(orderBy);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="orderBy"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static IOrderedEnumerable<T> OrderByIEnumerable<T, TK>(this IEnumerable<T> obj, Func<T, TK> orderBy, OrderingType orderingType) where T : class
        {
            if (orderBy == null)
                throw new Exception("OrderBy can not be Null！");

            return orderingType == OrderingType.Ascending ? obj.OrderBy(orderBy) : obj.OrderByDescending(orderBy);
        }

        //public static void TryLoad<T>(this EntityCollection<T> obj) where T : class, System.Data.Objects.DataClasses.IEntityWithRelationships
        //{
        //    if (!obj.IsLoaded) { obj.Load(); }
        //}

        //public static void TryLoad<T>(this EntityReference<T> obj) where T : class, System.Data.Objects.DataClasses.IEntityWithRelationships
        //{
        //    if (!obj.IsLoaded) { obj.Load(); }
        //}
    }
}
