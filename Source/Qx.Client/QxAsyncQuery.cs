using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Qx
{
    public class QxAsyncQuery<T> : IAsyncQueryable<T>, IAsyncQueryProvider
    {
        private readonly IAsyncQueryServiceProvider _service;
        private readonly Expression _expression;

        internal QxAsyncQuery(IAsyncQueryServiceProvider service, Expression expression) => (_service, _expression) = (service, expression);

        Type IAsyncQueryable.ElementType => typeof(T);

        Expression IAsyncQueryable.Expression => _expression;

        IAsyncQueryProvider IAsyncQueryable.Provider => this;

        IAsyncQueryable<TElement> IAsyncQueryProvider.CreateQuery<TElement>(Expression expression) => new QxAsyncQuery<TElement>(_service, expression);

        ValueTask<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken token) => _service.GetAsyncResult<TResult>(expression, token);

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token) => _service.GetAsyncEnumerator<T>(_expression, token);
    }
}
