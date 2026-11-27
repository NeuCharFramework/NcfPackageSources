/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcExpressionHelper.cs
    File description: SenparcExpressionHelper related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.21.3.2-preview6 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Provides fluent construction of conditional LINQ predicates.
    ///
    ///   var seh = new SenparcExpressionHelper<T>();
    ///   seh.ValueCompare
    ///       .AndAlso(true, i => !i.IsDeleted && i.MPAccountId == MPAccount.Id)
    ///       .AndAlso(!string.IsNullOrEmpty(SearchText), i => i.MPKeywordList.Count(c => c.Content.Contains(SearchText)) > 0);
    ///   var where = seh.BuildWhereExpression();
    ///
    /// </summary>
    /// <typeparam name="TEntity">The entity type represented by the composed predicates.</typeparam>
    public class SenparcExpressionHelper<TEntity> where TEntity : class
    {
        public ValueCompare<TEntity> ValueCompare { get; set; }
        /// <summary>
        /// Initializes a helper with an empty conditional predicate.
        /// </summary>
        public SenparcExpressionHelper()
        {
            ValueCompare = new ValueCompare<TEntity>();
        }

        /// <summary>
        /// Returns the current composed predicate, or an always-true predicate when no filters were added.
        /// </summary>
        /// <returns>
        /// The composed predicate, or a predicate that always returns <see langword="true"/>
        /// when no conditional expression has been added.
        /// </returns>
        public Expression<Func<TEntity, bool>> BuildWhereExpression()
        {
            if (ValueCompare.ExpressionBody == null)
            {
                //Legacy comment translated to English.
                Expression<Func<TEntity, bool>> returnTrue = z => true;
                return returnTrue;
            }
            var where = ValueCompare.ExpressionBody;
            return where;
        }
    }
    /// <summary>
    /// Builds a conditional predicate by composing entity expressions with logical operators.
    /// </summary>
    /// <typeparam name="TEntity">The entity type accepted by each predicate.</typeparam>
    public class ValueCompare<TEntity>
    {
        public Expression<Func<TEntity, bool>> ExpressionBody { get; set; }
        /// <summary>
        /// Initializes an empty predicate composition.
        /// </summary>
        public ValueCompare()
        {
        }
        /// <summary>
        /// Adds <paramref name="filter"/> with logical conjunction when the condition is enabled.
        /// </summary>
        /// <param name="combineWhenTrue">Whether the filter should be included.</param>
        /// <param name="filter">The entity predicate to add.</param>
        /// <returns>This comparer for fluent composition.</returns>
        public ValueCompare<TEntity> AndAlso(bool combineWhenTrue, Expression<Func<TEntity, bool>> filter)
        {
            return Combine(combineWhenTrue, filter, Expression.AndAlso);
        }
        /// <summary>
        /// Adds <paramref name="filter"/> with logical disjunction when the condition is enabled.
        /// </summary>
        /// <param name="combineWhenTrue">Whether the filter should be included.</param>
        /// <param name="filter">The entity predicate to add.</param>
        /// <returns>This comparer for fluent composition.</returns>
        public ValueCompare<TEntity> OrElse(bool combineWhenTrue, Expression<Func<TEntity, bool>> filter)
        {
            return Combine(combineWhenTrue, filter, Expression.OrElse);
        }

        /// <summary>
        /// Replaces the current predicate with its logical negation.
        /// </summary>
        /// <returns>This comparer for fluent composition.</returns>
        public ValueCompare<TEntity> Not()
        {
            var negated = Expression.Not(ExpressionBody.Body);
            ExpressionBody = Expression.Lambda<Func<TEntity, bool>>(negated, ExpressionBody.Parameters);
            return this;
        }
        /// <summary>
        /// Adds a predicate, using the supplied expression merger when a prior predicate exists.
        /// </summary>
        /// <param name="filter">The predicate to add.</param>
        /// <param name="merge">The expression operation used to combine predicate bodies.</param>
        /// <returns>This comparer for fluent composition.</returns>
        public ValueCompare<TEntity> Combine(Expression<Func<TEntity, bool>> filter, Func<Expression, Expression, Expression> merge)
        {
            if (ExpressionBody == null)
            {
                ExpressionBody = PredicateBuilder.Create(filter);
                return this;
            }
            ExpressionBody = ExpressionBody.Compose(filter, merge);

            return this;
        }
        /// <summary>
        /// Conditionally adds a predicate when the controlling flag is <see langword="true"/>.
        /// </summary>
        /// <param name="combineWhenTrue">Whether the predicate should be added.</param>
        /// <param name="filter">The predicate to add when enabled.</param>
        /// <param name="merge">The expression operation used to combine predicate bodies.</param>
        /// <returns>This comparer for fluent composition.</returns>
        public ValueCompare<TEntity> Combine(bool combineWhenTrue, Expression<Func<TEntity, bool>> filter, Func<Expression, Expression, Expression> merge)
        {
            if (!combineWhenTrue)
            {
                return this;
            }
            return Combine(filter, merge);
        }
    }
    public static class PredicateBuilder
    {
        /// <summary>
        /// Returns the supplied predicate so it can participate in the composition pipeline.
        /// </summary>
        /// <param name="predicate">The predicate expression to preserve.</param>
        /// <returns>The same predicate expression.</returns>
        public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate) { return predicate; }

        /// <summary>
        /// Rebinds the second expression's parameters and combines both bodies with the supplied merger.
        /// </summary>
        /// <param name="first">The expression whose parameters are retained.</param>
        /// <param name="second">The expression whose parameters are replaced.</param>
        /// <param name="merge">The operation used to combine the two expression bodies.</param>
        /// <returns>A composed expression using the parameter list from <paramref name="first"/>.</returns>
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // zip parameters (map from parameters of second to parameters of first)
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with the parameters in the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // create a merged lambda expression with parameters from the first expression
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// ParameterRebinder
        /// </summary>
        class ParameterRebinder : ExpressionVisitor
        {
            /// <summary>
            /// The ParameterExpression map
            /// </summary>
            readonly Dictionary<ParameterExpression, ParameterExpression> map;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParameterRebinder"/> class.
            /// </summary>
            /// <param name="map">The map.</param>
            ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            /// <summary>
            /// Replaces the parameters.
            /// </summary>
            /// <param name="map">The map.</param>
            /// <param name="exp">The exp.</param>
            /// <returns>Expression</returns>
            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            /// <summary>
            /// Visits the parameter.
            /// </summary>
            /// <param name="p">The p.</param>
            /// <returns>Expression</returns>
            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if (map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }
    }
}
