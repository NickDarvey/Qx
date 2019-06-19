using System;
using System.Linq;
using System.Linq.Expressions;

namespace Qx
{
    public class QxAsyncQueryClient : IAsyncQueryClient
    {

        private readonly IAsyncQueryServiceProvider _service;

        public QxAsyncQueryClient(IAsyncQueryServiceProvider service) => _service = service;

        public IAsyncQueryable<TElement> GetEnumerable<TElement>(string name) =>
            GetEnumerable<TElement>(Expression.Parameter(typeof(Func<IAsyncQueryable<TElement>>), name));

        public Func<TArg, IAsyncQueryable<TElement>> GetEnumerable<TArg, TElement>(string name) =>
            arg => GetEnumerable<TElement>(
                Expression.Parameter(typeof(Func<TArg, IAsyncQueryable<TElement>>), name),
                Expression.Constant(arg, typeof(TArg)));

        private IAsyncQueryable<TElement> GetEnumerable<TElement>(ParameterExpression parameter, params Expression[] arguments) =>
            new QxAsyncQuery<TElement>(_service, Expression.Invoke(parameter, arguments));

        // For future reference,
        // something like this here would mean all of the 'standard' expression building bits would
        // be encapsulated entirely within this QxClient class.
        ///// <summary>
        ///// Normalizes expressions before passing them to the actual service provider.
        ///// </summary>
        //private class NormalizingAsyncQueryServiceProvider : IAsyncQueryServiceProvider
        //{
        //    private readonly IAsyncQueryServiceProvider _base;

        //    public NormalizingAsyncQueryServiceProvider(IAsyncQueryServiceProvider service) => _base = service;

        //    public IAsyncEnumerator<T> GetAsyncEnumerator<T>(Expression expression, CancellationToken token)
        //    {
        //        var reduced = new SomeKindaReducer().Visit(expression);
        //        return _base.GetAsyncEnumerator<T>(reduced, token);
        //    }

        //    public ValueTask<T> GetAsyncResult<T>(Expression expression, CancellationToken token)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
    }
}
