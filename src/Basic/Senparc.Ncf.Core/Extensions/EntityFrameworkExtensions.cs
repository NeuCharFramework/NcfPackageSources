/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityFrameworkExtensions.cs
    File description: EntityFrameworkExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Ncf.Core.Extensions
{
    public class EntityFrameworkExtensions
    {
        /// <summary>
        /// Builds a predicate that compares a selected property against any supplied value.
        /// </summary>
        /// <typeparam name="TElement">The queried element type.</typeparam>
        /// <typeparam name="TValue">The selected property and comparison value type.</typeparam>
        /// <param name="valueSelector">The property selector.</param>
        /// <param name="values">The accepted values.</param>
        /// <returns>An OR-composed equality predicate, or an always-false predicate for no values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the selector or values sequence is null.</exception>
        public static Expression<Func<TElement, bool>> BuildContainsExpression<TElement, TValue>(Expression<Func<TElement, TValue>> valueSelector, IEnumerable<TValue> values)
        {
            if (null == valueSelector)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }

            if (null == values)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ParameterExpression p = valueSelector.Parameters.Single();

            // p => valueSelector(p) == values[0] || valueSelector(p) == ...

            if (!values.Any())
            {
                return e => false;
            }

            var equals = values.Select(value => (Expression)Expression.Equal(valueSelector.Body, Expression.Constant(value, typeof(TValue))));

            var body = equals.Aggregate<Expression>((accumulate, equal) => Expression.Or(accumulate, equal));

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
    }
}
