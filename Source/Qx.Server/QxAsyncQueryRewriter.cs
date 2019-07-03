using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Qx
{
    public static class QxAsyncQueryRewriter
    {
        private delegate InvocationExpression InvocationFactory(IEnumerable<Expression> arguments);

        /// <summary>
        /// Rewrites a Qx query (an expression tree with unbound <see cref="IAsyncQueryable{T}"/> parameters) binding it to the provided queryable factories
        /// and creating an expression to which you can pass synthetic arguments to the factories.
        /// </summary>
        /// <typeparam name="TArg">A synthetic argument.</typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expression"></param>
        /// <param name="queryables"></param>
        /// <returns></returns>
        public static Expression<Func<TArg, TResult>> Rewrite<TArg, TResult>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> queryables) =>
            Rewrite<Func<TArg, TResult>>(expression, queryables, new[] { Expression.Parameter(typeof(TArg)) });

        public static Expression<Func<TResult>> Rewrite<TResult>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> queryables) =>
            Rewrite<Func<TResult>>(expression, queryables, Enumerable.Empty<ParameterExpression>());

        // TODO: etc

        private static Expression<TDelegate> Rewrite<TDelegate>(Expression expression, IReadOnlyDictionary<string, LambdaExpression> nameBindings, IEnumerable<ParameterExpression> syntheticParameters)
        {
            var unboundParameters = QxAsyncQueryScanner.FindUnboundParameters(expression);

            var bindings = (from parameter in unboundParameters
                            join binding in nameBindings on parameter.Name equals binding.Key into pairs
                            from pair in pairs.DefaultIfEmpty()
                            select (Parameter: parameter, Implementation: pair.Value)).ToDictionary(kv => kv.Parameter, kv => kv.Implementation);

            // TODO: less throwing plz

            foreach(var binding in bindings)
            {
                if (binding.Value == default) throw new InvalidOperationException("Some error about there being an unbound parameter");
                if (binding.Key.Type == binding.Value.Type) continue; // An exact match
                //if (binding.Key.Type.IsGenericType == false || binding.Key.Type.GetGenericTypeDefinition()) // TODO: Some kind of check to make sure we're actually dealing with a Func of whatever arity
                var originalParameterTypesWithSyntheticParameterTypes = binding.Key.Type.GetGenericArguments().SkipLast(1).Concat(syntheticParameters.Select(p => p.Type));
                var implementationParameterTypes = binding.Value.Parameters.Select(p => p.Type);
                if (originalParameterTypesWithSyntheticParameterTypes.SequenceEqual(implementationParameterTypes) == false) throw new InvalidOperationException("Some error about params not matching");
            }

            var bindingFactories = bindings.ToDictionary(kv => kv.Key, kv => CreateInvocationFactory(kv.Value, syntheticParameters));

            return Expression.Lambda<TDelegate>(new Impl(bindingFactories).Visit(expression), syntheticParameters);
        }

        private static InvocationFactory CreateInvocationFactory(LambdaExpression expr, IEnumerable<ParameterExpression> parameters) =>
            args => Expression.Invoke(expr, args.Concat(parameters));

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
