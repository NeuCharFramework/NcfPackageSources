/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EntityFrameworkExtensions.cs
    File description: EntityFrameworkExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Ncf.Core.Extensions
{
    public class EntityFrameworkExtensions
    {
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