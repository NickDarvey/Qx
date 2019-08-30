// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)
// Based on https://github.com/mattwar/iqtoolkit/blob/master/License.txt

using Qx.Internals;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Qx.Client.Rewriters
{
    /// <summary>
    /// Rewrites an expression tree so that locally isolatable sub-expressions are evaluated and converted into ConstantExpression nodes.
    /// </summary>
    internal static class PartialEvaluationRewriter
    {
        /// <summary>
        /// Performs evaluation and replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression Rewrite(Expression expression) =>
            Evaluator.Evaluate(Nominator.Nominate(expression), expression);

        /// <summary>
        /// Evaluates and replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        private class Evaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            private Evaluator(HashSet<Expression> candidates) => _candidates = candidates;

            public static Expression Evaluate(HashSet<Expression> candidates, Expression expression) =>
                new Evaluator(candidates).Visit(expression);

            public override Expression Visit(Expression exp)
            {
                if (_candidates.Contains(exp))
                {
                    return Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression PostEval(ConstantExpression e)
            {
                return e;
            }

            private Expression Evaluate(Expression e)
            {
                Type type = e.Type;
                if (e.NodeType == ExpressionType.Convert)
                {
                    // check for unnecessary convert & strip them
                    var u = (UnaryExpression)e;
                    if (u.Operand.Type.GetNonNullableType() == type.GetNonNullableType())
                    {
                        e = ((UnaryExpression)e).Operand;
                    }
                }
                if (e.NodeType == ExpressionType.Constant)
                {
                    // in case we actually threw out a nullable conversion above, simulate it here
                    // don't post-eval nodes that were already constants
                    if (e.Type == type)
                    {
                        return e;
                    }
                    else if (e.Type.GetNonNullableType() == type.GetNonNullableType())
                    {
                        return Expression.Constant(((ConstantExpression)e).Value, type);
                    }
                }
                var me = e as MemberExpression;
                if (me != null)
                {
                    // member accesses off of constant's are common, and yet since these partial evals
                    // are never re-used, using reflection to access the member is faster than compiling  
                    // and invoking a lambda
                    var ce = me.Expression as ConstantExpression;
                    if (ce != null)
                    {
                        return this.PostEval(Expression.Constant(me.Member.GetValue(ce.Value), type));
                    }
                }
                if (type.IsValueType)
                {
                    e = Expression.Convert(e, typeof(object));
                }
                Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(e);

                Func<object> fn = lambda.Compile();
                return this.PostEval(Expression.Constant(fn(), type));
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        private class Nominator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates = new HashSet<Expression>();
            
            private bool _cannotBeEvaluated;

            internal static HashSet<Expression> Nominate(Expression expression)
            {
                var nominator = new Nominator();
                nominator.Visit(expression);
                return nominator._candidates;
            }

            private static bool CanBeEvaluated(Expression node) => node.NodeType != ExpressionType.Parameter;

            protected override Expression VisitConstant(ConstantExpression c)
            {
                return base.VisitConstant(c);
            }

            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = _cannotBeEvaluated;
                    _cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this._cannotBeEvaluated)
                    {
                        if (CanBeEvaluated(expression))
                        {
                            _candidates.Add(expression);
                        }
                        else
                        {
                            _cannotBeEvaluated = true;
                        }
                    }
                    _cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression!;
            }
        }
    }
}