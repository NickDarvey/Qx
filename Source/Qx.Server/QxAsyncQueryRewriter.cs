using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Qx
{
    public static class QxAsyncQueryRewriter
    {
        //private delegate InvocationExpression InvocationFactory(IEnumerable<Expression> arguments);

        ///// <summary>
        ///// Rewrites a Qx query (an expression tree with unbound <see cref="IAsyncQueryable{T}"/> parameters) binding it to the provided queryable factories
        ///// and creating an expression to which you can pass synthetic arguments to the factories.
        ///// </summary>
        ///// <typeparam name="TArg">A synthetic argument.</typeparam>
        ///// <typeparam name="TResult"></typeparam>
        ///// <param name="expression"></param>
        ///// <param name="queryables"></param>
        ///// <returns></returns>
        //public static Expression<Func<TArg, TResult>> Rewrite<TArg, TResult>(Expression expression, IReadOnlyDictionary<ParameterExpression, LambdaExpression> queryables) =>
        //    Rewrite<Func<TArg, TResult>>(expression, queryables, new[] { Expression.Parameter(typeof(TArg)) });

        //public static Expression<Func<TResult>> Rewrite<TResult>(Expression expression, IReadOnlyDictionary<ParameterExpression, LambdaExpression> queryables) =>
        //    Rewrite<Func<TResult>>(expression, queryables, Enumerable.Empty<ParameterExpression>());

        //// TODO: etc

        //private static Expression<TDelegate> Rewrite<TDelegate>(Expression expression, IReadOnlyDictionary<ParameterExpression, LambdaExpression> bindings, IEnumerable<ParameterExpression> syntheticParameters)
        //{
        //    // TODO: less throwing plz

        //    var bindingFactories = new Dictionary<ParameterExpression, InvocationFactory>();
        //    foreach(var binding in bindings)
        //    {
        //        if (binding.Value == default) throw new InvalidOperationException("Some error about there being an unbound parameter");

        //        if (binding.Key.Type == binding.Value.Type)
        //        {
        //            bindingFactories[binding.Key] = args => Expression.Invoke(binding.Value, args);
        //        }

        //        else // with synthetic params
        //        {
        //            //if (binding.Key.Type.IsGenericType == false || binding.Key.Type.GetGenericTypeDefinition()) // TODO: Some kind of check to make sure we're actually dealing with a Func of whatever arity
        //            var originalParameterTypesWithSyntheticParameterTypes = binding.Key.Type.GetGenericArguments().SkipLast(1).Concat(syntheticParameters.Select(p => p.Type));
        //            var implementationParameterTypes = binding.Value.Parameters.Select(p => p.Type);
        //            if (originalParameterTypesWithSyntheticParameterTypes.SequenceEqual(implementationParameterTypes) == false) throw new InvalidOperationException("Some error about params not matching");

        //            bindingFactories[binding.Key] = args => Expression.Invoke(binding.Value, args.Concat(syntheticParameters));
        //        }
        //    }

        //    return Expression.Lambda<TDelegate>(new Impl(bindingFactories).Visit(expression), syntheticParameters);
        //}

        //private static InvocationFactory CreateInvocationFactory(LambdaExpression expr, IEnumerable<ParameterExpression> parameters) =>
        //    args => Expression.Invoke(expr, args.Concat(parameters));

        public delegate InvocationExpression InvocationFactory(IEnumerable<Expression> args);

        public static Expression Rewrite(Expression expression, IReadOnlyDictionary<ParameterExpression, InvocationFactory> bindings) =>
            new Impl(bindings).Visit(expression);

        private class Impl : ExpressionVisitor
        {
            private readonly IReadOnlyDictionary<ParameterExpression, InvocationFactory> _bindings;

            public Impl(IReadOnlyDictionary<ParameterExpression, InvocationFactory> bindings) =>
                _bindings = bindings;

            protected override Expression VisitInvocation(InvocationExpression node)
            {
                if (!(node.Expression is ParameterExpression parameter))
                    return base.VisitInvocation(node);

                if (!(_bindings.TryGetValue(parameter, out var createInvocation)))
                    throw new InvalidOperationException($"No binding provided for parameter '{parameter}'");

                return createInvocation(node.Arguments);
            }
        }
    }
}
