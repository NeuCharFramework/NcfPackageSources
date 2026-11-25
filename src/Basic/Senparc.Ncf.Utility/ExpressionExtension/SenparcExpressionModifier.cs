/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcExpressionModifier.cs
    File description: SenparcExpressionModifier related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Visits expression nodes and accumulates visited binary predicates with logical conjunction.
    /// </summary>
    /// <typeparam name="TEntity">The entity type represented by the resulting predicate.</typeparam>
    public class SenparcExpressionModifier<TEntity> : ExpressionVisitor
    {
        public BinaryExpression BE { get; set; }

        /// <summary>
        /// Initializes an empty expression modifier.
        /// </summary>
        public SenparcExpressionModifier()
        { }

        /// <summary>
        /// Visits the supplied expressions and accumulates their binary predicates.
        /// </summary>
        /// <param name="expressions">The expressions to visit in order.</param>
        /// <returns>The visited expressions returned by the base visitor.</returns>
        public ReadOnlyCollection<Expression> AndAlso(IList<Expression> expressions)
        {
            ReadOnlyCollection<Expression> col = new ReadOnlyCollection<Expression>(expressions);
            var result = Visit(col);
            return result;
        }

        /// <summary>
        /// Creates an entity predicate from the binary expression accumulated during visitation.
        /// </summary>
        /// <returns>The generated entity predicate.</returns>
        public Expression<Func<TEntity, bool>> BuildWhereExpression()
        {
            ParameterExpression pe = Expression.Parameter(typeof(string), "z");

            Expression<Func<TEntity, bool>> where = Expression.Lambda<Func<TEntity, bool>>(BE, pe);
            return where;
        }

        /// <summary>
        /// Records a binary expression and combines subsequent non-lifted expressions with <c>AndAlso</c>.
        /// </summary>
        /// <param name="b">The binary expression being visited.</param>
        /// <returns>The expression returned by the base visitor.</returns>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            //if (b.NodeType == ExpressionType.AndAlso)
            //{
            //    Expression left = this.Visit(b.Left);
            //    Expression right = this.Visit(b.Right);

            //    // Make this binary expression an OrElse operation instead of an AndAlso operation.
            //    return Expression.MakeBinary(ExpressionType.OrElse, left, right, b.IsLiftedToNull, b.Method);
            //}

            if (b.IsLifted)
            {
                return base.VisitBinary(b);
            }
            if (BE == null)
            {
                BE = b;
            }
            else
            {
                //Legacy comment translated to English.
                BE = Expression.MakeBinary(ExpressionType.AndAlso, BE, b);
            }

            return base.VisitBinary(b);
        }
    }
}
