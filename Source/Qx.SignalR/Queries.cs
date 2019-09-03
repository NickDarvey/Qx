using Microsoft.AspNetCore.SignalR;
using Qx.Prelude;
using Qx.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Qx.Rewriters;
using static Qx.SignalR.Binders;
using static Qx.SignalR.Rewriters;

namespace Qx.SignalR
{
    /// <summary>
    /// A collection of functions for creating a queryable hub.
    /// </summary>
    public static class Queries
    {
        public static ValueTask<Validation<string, Func<CancellationToken, IAsyncQueryable<object>>>> CompileEnumerableQuery<TSourceDescription>(
            Expression expression,
            Verifier verify,
            Authorizer<TSourceDescription> authorize,
            IReadOnlyDictionary<string, TSourceDescription> bindings) where TSourceDescription : IQueryableSourceDescription =>
            CompileQuery<TSourceDescription, IAsyncQueryable<object>>(expression, verify, authorize, bindings, RewriteManyResultsType);

        public static ValueTask<Validation<string, Func<CancellationToken, Task<object>>>> CompileExecutableQuery<TSourceDescription>(
            Expression expression,
            Verifier verify,
            Authorizer<TSourceDescription> authorize,
            IReadOnlyDictionary<string, TSourceDescription> bindings) where TSourceDescription : IQueryableSourceDescription =>
            CompileQuery<TSourceDescription, Task<object>>(expression, verify, authorize, bindings, RewriteSingleResultsType);
    
        internal static ValueTask<Validation<string, Func<CancellationToken, TResult>>> CompileQuery<TSourceDescription, TResult>(
            Expression expression,
            Verifier verify,
            Authorizer<TSourceDescription> authorize,
            IReadOnlyDictionary<string, TSourceDescription> bindings,
            Func<Expression, Expression> boxingRewriter) where TSourceDescription : IQueryableSourceDescription =>
            from verified in verify(expression).ToValueTask()
            let unboundParameters = Scanners.FindUnboundParameters(expression)
            from methodBindings in BindMethods(unboundParameters, bindings).ToValueTask()
            from authorized in authorize(methodBindings.Values)
            let lambdaBindings = BindLambdas(methodBindings)
            let syntheticParameters = new[] { Expression.Parameter(typeof(CancellationToken)) }
            from invocationBindings in BindInvocations(lambdaBindings, syntheticParameters).ToValueTask()
            let boundQuery = BindingRewriter.Rewrite(expression, invocationBindings)
            let boxedQuery = boxingRewriter(boundQuery)
            select Expression.Lambda<Func<CancellationToken, TResult>>(boxedQuery, syntheticParameters).Compile();
    }
}
