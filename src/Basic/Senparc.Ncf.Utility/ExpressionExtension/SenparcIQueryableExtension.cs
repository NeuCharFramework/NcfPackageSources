/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcIQueryableExtension.cs
    File description: SenparcIQueryableExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Utility.ExpressionExtension
{
    /// <summary>
    /// Provides dynamic ordering for LINQ queryables using a comma-separated field specification.
    /// </summary>
    public static class SenparcIQueryableExtension
    {
        const string OrderByDescendingFuncName = "OrderByDescending";

        const string OrderByFuncName = "OrderBy";

        const string ThenByFuncName = "ThenBy";

        const string ThenByDescendingFuncName = "ThenByDescending";

        const string Asc = "asc";

        const string Desc = "desc";

        /// <summary>
        /// Applies one or more property-based ordering clauses to a queryable.
        /// Clauses use the form <c>PropertyName [asc|desc]</c> and are separated by commas.
        /// Unknown properties are skipped.
        /// </summary>
        /// <typeparam name="T">The element type queried.</typeparam>
        /// <param name="iQueryable">The source queryable.</param>
        /// <param name="orderBy">The comma-separated ordering specification.</param>
        /// <returns>The queryable with the requested ordering expressions applied.</returns>
        /// <exception cref="ArgumentNullException">Thrown by the query provider when the source queryable is null.</exception>
        /// <exception cref="InvalidOperationException">May be thrown when no valid property remains for a non-empty ordering specification.</exception>
        public static IQueryable<T> OrderByExtension<T>(this IQueryable<T> iQueryable, string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
            {
                return iQueryable;
            }

            Type type = typeof(T);
            MethodCallExpression OrderExpression = null;
            string[] orderField = orderBy.Trim().Split(",");
            foreach (var item in orderField)
            {
                string[] singleField = item.Trim().Split(" ");
                string fieldName = singleField[0];
                string orderby = OrderByFuncName;

                ParameterExpression param = Expression.Parameter(type, "_");
                PropertyInfo property = type.GetProperty(fieldName);
                if (property == null)
                {
                    continue;
                }

                if (singleField.Length == 2)
                {
                    orderby = singleField[1].ToLower();
                }
                Expression propertyAccessExpression = Expression.Property(param, property);
                LambdaExpression le = Expression.Lambda(propertyAccessExpression, param);

                if (OrderExpression == null)
                {
                    string funcName = OrderByFuncName;
                    if (!string.IsNullOrEmpty(orderby) && orderby == Desc)
                    {
                        funcName = OrderByDescendingFuncName;
                    }
                    OrderExpression = Expression.Call(typeof(Queryable), funcName, new Type[] { type, property.PropertyType }, iQueryable.Expression, le);
                }
                else
                {
                    string funcName = ThenByFuncName;
                    if (!string.IsNullOrEmpty(orderby) && orderby == Desc)
                    {
                        funcName = ThenByDescendingFuncName;
                    }
                    OrderExpression = Expression.Call(typeof(Queryable), funcName, new Type[] { type, property.PropertyType }, OrderExpression, le);
                }

            }
            return iQueryable.Provider.CreateQuery<T>(OrderExpression);
        }
    }
}
