/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ObjectQueryExtensions.cs
    File description: ObjectQueryExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Ncf.Core.Extensions
{
    /// <summary>
    /// Contains query-extension helpers used by NCF for optional EF Core includes
    /// and direction-aware ordering.
    /// </summary>
    public static class ObjectQueryExtensions
    {
        /// <summary>
        /// Applies each supplied navigation-property path to an EF Core query.
        /// </summary>
        /// <typeparam name="T">The queried entity type.</typeparam>
        /// <param name="obj">
        /// The query to extend. Its existing query provider remains responsible for
        /// translating the resulting include operations.
        /// </param>
        /// <param name="includes">
        /// Navigation-property paths accepted by
        /// <see cref="EntityFrameworkQueryableExtensions.Include{TEntity}(IQueryable{TEntity}, string)"/>.
        /// A <see langword="null"/> array leaves the query unchanged.
        /// </param>
        /// <returns>The query with each supplied include path applied in order.</returns>
        /// <exception cref="ArgumentNullException">
        /// Propagated by EF Core when <paramref name="obj"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static IQueryable<T> Includes<T>(this IQueryable<T> obj, string[] includes) where T : class
        {
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
        /// Applies navigation-property includes identified by their types.
        /// </summary>
        /// <typeparam name="T">The queried entity type.</typeparam>
        /// <param name="obj">The query to extend.</param>
        /// <param name="includeTyles">
        /// Types whose names identify navigation properties on
        /// <typeparamref name="T"/>. A <see langword="null"/> array leaves the
        /// query unchanged.
        /// </param>
        /// <returns>The query with each requested navigation property included in order.</returns>
        /// <exception cref="ArgumentNullException">
        /// Propagated by EF Core when <paramref name="obj"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static IQueryable<T> Includes<T>(this IQueryable<T> obj, Type[] includeTyles) where T : class
        {
            if (includeTyles == null)
                return obj;

            foreach (var item in includeTyles)
            {
                obj = obj.Include(item);
            }
            return obj;
        }

        /// <summary>
        /// Applies one navigation-property type as a query include.
        /// </summary>
        /// <typeparam name="T">The queried entity type.</typeparam>
        /// <param name="obj">The query to extend.</param>
        /// <param name="includeType">
        /// The type whose <see cref="Type.Name"/> is used as the navigation-property name.
        /// </param>
        /// <returns>The query with the named navigation property included.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="includeType"/> is
        /// <see langword="null"/>, because its name is read directly.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Propagated by EF Core when <paramref name="obj"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static IQueryable<T> Include<T>(this IQueryable<T> obj, Type includeType) where T : class
        {
            return obj.Include(includeType.Name);
        }

        /// <summary>
        /// Orders an EF Core query in the requested direction.
        /// </summary>
        /// <typeparam name="T">The queried entity type.</typeparam>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="obj">The query to order.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <returns>An ordered query.</returns>
        /// <exception cref="Exception">
        /// Thrown when <paramref name="orderBy"/> is <see langword="null"/>. The
        /// current implementation uses the historical generic exception type.
        /// </exception>
        public static IOrderedQueryable<T> OrderBy<T, TK>(this IQueryable<T> obj, Expression<Func<T, TK>> orderBy, OrderingType orderingType) where T : class
        {
            if (orderBy == null)
                throw new Exception("OrderBy can not be Null！");

            return orderingType == OrderingType.Ascending ? obj.OrderBy(orderBy) : obj.OrderByDescending(orderBy);
        }

        /// <summary>
        /// Orders an in-memory sequence in the requested direction.
        /// </summary>
        /// <typeparam name="T">The sequence element type.</typeparam>
        /// <typeparam name="TK">The ordering key type.</typeparam>
        /// <param name="obj">The sequence to order.</param>
        /// <param name="orderBy">The ordering function.</param>
        /// <param name="orderingType">The requested ordering direction.</param>
        /// <returns>An ordered sequence.</returns>
        /// <exception cref="Exception">
        /// Thrown when <paramref name="orderBy"/> is <see langword="null"/>. The
        /// current implementation uses the historical generic exception type.
        /// </exception>
        public static IOrderedEnumerable<T> OrderByIEnumerable<T, TK>(this IEnumerable<T> obj, Func<T, TK> orderBy, OrderingType orderingType) where T : class
        {
            if (orderBy == null)
                throw new Exception("OrderBy can not be Null！");

            return orderingType == OrderingType.Ascending ? obj.OrderBy(orderBy) : obj.OrderByDescending(orderBy);
        }

    }
}
