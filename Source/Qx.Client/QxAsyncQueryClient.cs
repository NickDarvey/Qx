using Qx.Client.Rewriters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx.Client
{
    public class QxAsyncQueryClient : IAsyncQueryClient
    {
        private readonly IAsyncQueryServiceProvider _service;

        public QxAsyncQueryClient(IAsyncQueryServiceProvider serviceProvider) =>
            _service = new NormalizingAsyncQueryServiceProvider(@base: serviceProvider);

        public IAsyncQueryable<TElement> GetEnumerable<TElement>(string name) =>
            GetEnumerable<TElement>(Expression.Parameter(typeof(Func<IAsyncQueryable<TElement>>), name));

        public Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name) =>
            arg => GetEnumerable<TElement>(
                Expression.Parameter(typeof(Func<TArg, IAsyncQueryable<TElement>>), name),
                Expression.Constant(arg, typeof(TArg)));

        public Func<TArg1, TArg2, IAsyncQueryable<TElement>> GetEnumerable<TArg1, TArg2, TElement>(string name) =>
            (arg1, arg2) => GetEnumerable<TElement>(
                Expression.Parameter(typeof(Func<TArg1, TArg2, IAsyncQueryable<TElement>>), name),
                Expression.Constant(arg1, typeof(TArg1)),
                Expression.Constant(arg2, typeof(TArg2)));

        private IAsyncQueryable<TElement> GetEnumerable<TElement>(ParameterExpression parameter, params Expression[] arguments) =>
            new QxAsyncQuery<TElement>(_service, Expression.Invoke(parameter, arguments));

        /// <summary>
        /// Normalizes expressions before passing them to the actual service provider.
        /// </summary>
        private class NormalizingAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        {
            private readonly IAsyncQueryServiceProvider _base;

            public NormalizingAsyncQueryServiceProvider(IAsyncQueryServiceProvider @base) => _base = @base;

            public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token) =>
                _base.GetAsyncEnumerator<T>(Normalize(expression), token);

            public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token) =>
                _base.GetAsyncResult<T>(Normalize(expression), token);

            private Expression Normalize(Expression expression) =>
                AnonymousTypeRewriter.Rewrite(PartialEvaluationRewriter.Rewrite(ClientCallRewriter.Rewrite(expression)));
        }
    }
}
